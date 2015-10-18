﻿using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IssueTracker.Models
{
    public class Comment : BaseEntity
    {
        // Foreign keys
        [Required]
        [Display(Name = "Issue")]
        [ForeignKey("Issue")]
        public Guid IssueId { get; set; }

        // Parameters
        [Required]
        [Display(Name = "Text")]
        public string Text { get; set; }

        [Display(Name = "Posted on")]
        public DateTime? Posted { get; set; }

        // Table definitions
        public ApplicationUser User { get; set; }

        public virtual Issue Issue { get; set; }
    }
}