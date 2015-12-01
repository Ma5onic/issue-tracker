﻿using IssueTracker.Core;
using IssueTracker.Core.Contracts;
using IssueTracker.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities
{
    public class State  : IIdentifiableEntity
    {
        // Parameters
        public Guid Id { get; set; }
        public string Title { get; set; }
        public bool IsInitial { get; set; }
        public string Colour { get; set; }
        public int OrderIndex { get; set; }

        // Table definitions
        public virtual ICollection<Issue> Issues { get; set; }

        public Guid EntityId
        {
            get { return Id; }
            set { Id = value; }
        }
    }
}
