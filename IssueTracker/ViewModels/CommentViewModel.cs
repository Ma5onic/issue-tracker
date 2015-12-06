﻿using IssueTracker.Data.Entities;
using System;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace IssueTracker.ViewModels
{
    public class CommentViewModel : BaseVersioningViewModel
    {
        // Foreign keys
        [Required]
        [Display(Name = "Issue")]
        public Guid IssueId { get; set; }

        [Required]
        public DateTime IssueCreatedAt { get; set; }

        // Parameters
        [Required]
        [Display(Name = "Text")]
        [AllowHtml]
        public string Text { get; set; }

        [Display(Name = "Posted on")]
        public DateTime? Posted { get; set; }

        [Display(Name = "Author")]
        public string AuthorId { get; set; }

        // Table definitions
        public virtual ApplicationUser User { get; set; }

        public virtual Issue Issue { get; set; }
        
    }
}