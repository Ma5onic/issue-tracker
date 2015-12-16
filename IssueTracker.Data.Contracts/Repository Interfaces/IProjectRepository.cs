﻿using Common.Data.Core.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IssueTracker.Entities;

namespace IssueTracker.Data.Contracts.Repository_Interfaces
{
    public interface IProjectRepository : IDataRepository<Project>
    {
        ICollection<Project> GetProjectsForUser(Guid userId);
    }
}
