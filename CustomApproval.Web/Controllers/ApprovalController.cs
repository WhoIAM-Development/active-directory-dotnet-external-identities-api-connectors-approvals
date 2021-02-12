using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CustomApproval.Web.Entities;
using CustomApproval.Web.Extensions;
using CustomApproval.Web.Models;
using CustomApproval.Web.Models.GraphApi;
using CustomApproval.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace CustomApproval.Web.Controllers
{
    public class ApprovalController : Controller
    {

        private readonly IUserService userService;
        private readonly IGraphSdkClientService newGraphService;
        private readonly IMailService mailService;
        private readonly GraphSettings graphSettings;
        private readonly AppSettings appSettings;

        public ApprovalController(
            IConfiguration config,
            IUserService userService,
            IGraphSdkClientService newGraphService,
            IMailService mailService)
        {
            this.userService = userService;
            this.newGraphService = newGraphService;
            this.mailService = mailService;

            graphSettings = config.GetSection("GraphApi")
              .Get<GraphSettings>();
            appSettings = config.GetSection("AppSettings")
              .Get<AppSettings>();
        }

        // GET: Approval
        public ActionResult Index()
        {

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Details(FindRequestModel data)
        {
            ViewBag.Email = data.Email;

            var users = await userService.GetUsersByEmail(data.Email);
            var groups = new List<GroupsModel>()
            {
                new GroupsModel()  {DisplayName = "Group1", Id = "abc" },
                new GroupsModel()  {DisplayName = "Group2", Id = "def" },
                new GroupsModel()  {DisplayName = "Group3", Id = "ghi" }
            };

            var detailsModel = new DetailsModel()
            {
                Users = users.ToUserModel(),
                Groups = groups
            };

            return View(detailsModel);
        }

        [HttpPost]
        public async Task<ActionResult> ApproveRequest(string Id, string[] SelectedGroups)
        {
            var user = await userService.GetUsersById(Id);

            ViewBag.Message = "An error occurred while updating the request.";

            if (user == null)
            {
                return View("Index");
            }

            if (user.IsSocialUser())
            {
                await newGraphService.CreateUser(user.SdkToSocialUserInput(graphSettings.Tenant));

                await mailService.SendApprovalNotification(user.Email, user.Locale);
            }
            else 
            {
                var result = await newGraphService.InviteGuestUser(user.Email);

                if (result == null || string.IsNullOrEmpty(result.invitedUser?.id))
                {
                    return View("Index");
                }

                await newGraphService.CreateUser(user.SdkToUpdateGuestUserInput());
            }

            await userService.RemoveUsersById(Id);

            ViewBag.Message = "Update was successful.";
            return View("Index");
        }

        [HttpPost]
        public async Task<ActionResult> DenyRequest(string Id)
        {
            await userService.UpdateUserStatusAsync(Id, Constants.UserApprovalStatus.Denied);

            ViewBag.Message = "Update was successful.";

            return View("Index");
        }
    }
}