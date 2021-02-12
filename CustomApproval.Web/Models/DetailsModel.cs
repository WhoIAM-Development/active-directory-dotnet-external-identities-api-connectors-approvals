using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CustomApproval.Web.Models
{
    public class DetailsModel
    {
        public IEnumerable<UserModel> Users { get; set; }
        public IEnumerable<GroupsModel> Groups { get; set; }
    }
}
