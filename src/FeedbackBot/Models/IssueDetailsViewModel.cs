using System;
using System.Collections.Generic;

namespace FeedbackBot.Models
{
    public class IssueDetailsViewModel
    {
        public IssueDetailsViewModel()
        {
            Comments = new List<CommentContainer>();
        }

        public IssueContainer Issue { get; set; }

        public List<CommentContainer> Comments { get; set; }

        public string VoteMessage { get; set; }
    }
}
