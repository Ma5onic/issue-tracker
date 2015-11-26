﻿using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using AutoMapper;
using IssueTracker.Entities;
using IssueTracker.ViewModels;
using PagedList;
using System.Collections.Generic;
using IssueTracker.Abstractions;
using Entities;
using IssueTracker.Data;

namespace IssueTracker.Controllers
{
    [AuthorizeOrErrorPage]
    public class IssuesController : Controller
    {
        private IssueTrackerContext db = new IssueTrackerContext();

        private const int ProjectsPerPage = 20;

        // GET: Issues
        public ActionResult Index(int? page, string sort)
        {
            // viewbag items are used in the header to sort the records
            ViewBag.CreatedSort = string.IsNullOrEmpty(sort) ? "created_desc" : string.Empty;
            ViewBag.SummarySort = sort == "summary" ? "summary_desc" : "summary";
            ViewBag.ReporterSort = sort == "reporter" ? "reporter_desc" : "reporter";
            ViewBag.ProjectSort = sort == "project" ? "project_desc" : "project";
            ViewBag.AssigneeSort = sort == "assignee" ? "assignee_desc" : "assignee";
            ViewBag.StatusSort = sort == "status" ? "status_desc" : "status";

            var issuesTemp = db.Issues
                .Where(n => n.Active)
                .GroupBy(n => n.Id)
                .Select(g => g.OrderByDescending(x => x.CreatedAt).FirstOrDefault())
                .Include(p => p.State).ToList()
                .OrderByDescending(x => x.Created);

            var issues = Mapper.Map<IEnumerable<IssueIndexViewModel>>(issuesTemp);
            issues = GetSortedIssues(issues, sort);

            var pageNumber = page ?? 1;
            return View(issues.ToPagedList(pageNumber, ProjectsPerPage));
        }

        private UsersByEmailComparer usersComparer = new UsersByEmailComparer();
        private ProjectsByTitleComparer projectsComparer = new ProjectsByTitleComparer();
        private StatesByTitleComparer statesComparer = new StatesByTitleComparer();

        private IEnumerable<IssueIndexViewModel> GetSortedIssues(IEnumerable<IssueIndexViewModel> issues, string sortKey)
        {
            switch (sortKey)
            {
                case "summary":
                    return issues.OrderBy(ii => ii.Name);
                case "summary_desc":
                    return issues.OrderByDescending(ii => ii.Name);
                case "reporter_desc":
                    return issues.OrderByDescending(ii => ii.Reporter, usersComparer);
                case "reporter":
                    return issues.OrderBy(ii => ii.Reporter, usersComparer);
                case "status_desc":
                    return issues.OrderByDescending(ii => ii.State, statesComparer);
                case "status":
                    return issues.OrderBy(ii => ii.State, statesComparer);
                case "assignee_desc":
                    return issues.OrderByDescending(ii => ii.Assignee, usersComparer);
                case "assignee":
                    return issues.OrderBy(ii => ii.Assignee, usersComparer);
                case "project_desc":
                    return issues.OrderByDescending(ii => ii.Project, projectsComparer);
                case "project":
                    return issues.OrderBy(ii => ii.Project, projectsComparer);
                case "created_desc":
                    return issues.OrderByDescending(ii => ii.Created);
                default:
                    return issues.OrderBy(ii => ii.Created);
            }
        }

        private class UsersByEmailComparer : IComparer<ApplicationUser>
        {
            public int Compare(ApplicationUser x, ApplicationUser y)
            {
                return x.Email.CompareTo(y.Email);
            }
        }

        private class ProjectsByTitleComparer : IComparer<ProjectViewModel>
        {
            public int Compare(ProjectViewModel x, ProjectViewModel y)
            {
                return x.Title.CompareTo(y.Title);
            }
        }

        private class StatesByTitleComparer : IComparer<StateViewModel>
        {
            public int Compare(StateViewModel x, StateViewModel y)
            {
                return x.Title.CompareTo(y.Title);
            }
        }

        // GET: Issues/Details/5
        public ActionResult Details(string id)
        {
            if (!Helper.CheckIssueCodePattern(id))
            {
                return new HttpStatusCodeResult((HttpStatusCode.BadRequest));
            }

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var codeSplitted = Helper.SplitIssueCode(id);
            var projectCode = codeSplitted[0];
            var issueNumber = int.Parse(codeSplitted[1]);

            if (projectCode == null || issueNumber == 0)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var issue = db.Issues
                .Where(i => i.Project.Code == projectCode && i.CodeNumber == issueNumber)
                .OrderByDescending(x => x.CreatedAt)
                .Include(i => i.State).First();

            var viewModel = new IssueDetailViewModel
            {
                Issue = Mapper.Map<IssueIndexViewModel>(issue)
            };

            if (viewModel.Issue == null)
            {
                return HttpNotFound();
            }

            // possible workflows
            /*
            changing this: var workflows = db.StateWorkflows.Where(c => c.FromState.Id == viewModel.Issue.State.Id);
            to this: var workflows = db.StateWorkflows.Where(c => c.FromStateId == viewModel.Issue.State.Id); 
            because i was getting this excpetion: The specified type member 'FromState' is not supported in LINQ to Entities. Only initializers, entity members, and entity navigation properties are supported
            */
            var workflows = db.StateWorkflows.Where(c => c.FromStateId == viewModel.Issue.State.Id);
            viewModel.StateWorkflows = Mapper.Map<IEnumerable<StateWorkflowViewModel>>(workflows);

            // comments from all versions of the issue
            var comments = db.Comments
                .Where(n => n.Active)
                .GroupBy(n => n.Id)
                .Select(g => g.OrderByDescending(x => x.CreatedAt).FirstOrDefault())
                .Where(n => n.IssueId == issue.Id)
                .OrderBy(n => n.Posted)
                .Include(n => n.User)
                .ToList();

            viewModel.Comments = Mapper.Map<IEnumerable<CommentViewModel>>(comments);
            foreach (var comment in viewModel.Comments)
            {
                comment.User = db.Users.Find(comment.AuthorId);
            }

            ViewBag.LoggedUser = getLoggedUser();
            ViewBag.IsUserAdmin = User.IsInRole(UserRoles.Administrators.ToString());
            ViewBag.ErrorMessage = TempData["ErrorMessage"] as string;
            return View(viewModel);
        }

        // GET: Issues/Create
        public ActionResult Create()
        {
            ViewBag.ErrorSQL = TempData["ErrorSQL"] as string;

            var projectsTemp = db.Projects
                .Where(n => n.Active)
                .GroupBy(n => n.Id)
                .Select(g => g.OrderByDescending(x => x.CreatedAt).FirstOrDefault())
                .ToList();
            ViewBag.AssigneeId = new SelectList(db.Users, "Id", "Email");
            ViewBag.ProjectId = new SelectList(projectsTemp, "Id", "Title");
            ViewBag.ReporterId = getLoggedUser().Id;

            return View();
        }

        // POST: Issues/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IssueCreateViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var initialState = getInitialState();
                if (initialState == null)
                {
                    TempData["ErrorSQL"] = "There is no initial state. The issue couldn't be created.";

                    return RedirectToAction("Create");
                }

                var projectTemp = db.Projects
                    .Where(x => x.Id == viewModel.ProjectId)
                    .OrderByDescending(x => x.CreatedAt).First();

                var issue = Mapper.Map<Issue>(viewModel);
                issue.StateId = initialState.Id;
                issue.ReporterId = getLoggedUser().Id;
                issue.Created = DateTime.Now;
                issue.ProjectCreatedAt = projectTemp.CreatedAt;
                issue.Id = Guid.NewGuid();
                issue.CodeNumber = db.Issues.Max(x => (int?)x.CodeNumber) + 1 ?? 1;

                db.Issues.Add(issue);
                db.SaveChanges();

                return RedirectToAction("Index");
            }

            ViewBag.AssigneeId = new SelectList(db.Users, "Id", "Email", viewModel.AssigneeId);
            ViewBag.ProjectId = new SelectList(db.Projects, "Id", "Title", viewModel.ProjectId);

            return View(viewModel);
        }

        private State getInitialState()
        {
            try
            {
                return db.States.First(s => s.IsInitial);
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        private ApplicationUser getLoggedUser()
        {
            if (User.Identity.IsAuthenticated)
            {
                ApplicationUser user = db.Users.First(dbUser => dbUser.Email == User.Identity.Name);
                return user;
            }
            return null;
        }

        // GET: Issues/Edit/5
        public ActionResult Edit(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var issue = db.Issues.Where(x => x.Id == id).OrderByDescending(x => x.CreatedAt).Include(x => x.Project).First();

            if (issue == null)
            {
                return HttpNotFound();
            }

            var projectsTemp = db.Projects
                .Where(n => n.Active)
                .GroupBy(n => n.Id)
                .Select(g => g.OrderByDescending(x => x.CreatedAt).FirstOrDefault())
                .ToList();

            var viewModel = Mapper.Map<IssueEditViewModel>(issue);

            ViewBag.AssigneeId = new SelectList(db.Users, "Id", "Email", issue.AssigneeId);
            ViewBag.ProjectId = new SelectList(projectsTemp, "Id", "Title", issue.ProjectId);
            ViewBag.StateId = new SelectList(db.States, "Id", "Title", issue.StateId);

            return View(viewModel);
        }

        // POST: Issues/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,Name,Description,AssigneeId,ProjectId")] IssueEditViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                // create a new entity
                var entityNew = db.Issues.AsNoTracking().Where(x => x.Id == viewModel.Id).OrderByDescending(x => x.CreatedAt).First();
                // in case the project was changed
                if (viewModel.ProjectId != entityNew.ProjectId)
                {
                    var projectTemp = db.Projects.Where(x => x.Id == viewModel.ProjectId).OrderByDescending(x => x.CreatedAt).First();
                    entityNew.ProjectCreatedAt = projectTemp.CreatedAt;
                }
                // map viewModel to the entity
                entityNew = Mapper.Map(viewModel, entityNew);
                // change CreatedAt
                entityNew.CreatedAt = DateTime.Now;
                // save the entity
                db.Issues.Add(entityNew);
                db.SaveChanges();

                return RedirectToAction("Details", new { id = entityNew.Code });

            }

            ViewBag.AssigneeId = new SelectList(db.Users, "Id", "Email", viewModel.AssigneeId);
            ViewBag.ProjectId = new SelectList(db.Projects, "Id", "Title", viewModel.ProjectId);

            return View(viewModel);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        public ActionResult ChangeStatus(Guid issueId, Guid to)
        {

            // create a new entity
            var entityNew = db.Issues.AsNoTracking().Where(x => x.Id == issueId).OrderByDescending(x => x.CreatedAt).First();
            if (entityNew != null)
            {
                // change status
                entityNew.StateId = to;
                // change CreatedAt
                entityNew.CreatedAt = DateTime.Now;
                // save the entity
                db.Issues.Add(entityNew);
                db.SaveChanges();
            }

            if (HttpContext.Request.UrlReferrer != null)
            {
                return Redirect(HttpContext.Request.UrlReferrer.AbsoluteUri);
            }

            return RedirectToAction("Index");
        }
    }
}
