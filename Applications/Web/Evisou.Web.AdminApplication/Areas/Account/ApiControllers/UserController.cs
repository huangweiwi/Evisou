﻿using Evisou.Account.Contract;
using Evisou.Framework.Utility;
using Evisou.Web.AdminApplication.Areas.Account.WebApiModels;
using Evisou.Web.AdminApplication.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Evisou.Web;
using System.Linq.Expressions;

namespace Evisou.Web.AdminApplication.Areas.Account.ApiControllers
{
   
     [WebApiPermission(EnumBusinessPermission.AccountManage_User)]
    public class UserController : AdminApiControllerBase
    {
        public HttpResponseMessage Get([FromUri] UserInquiryDTO userInquiryDTO)
        {

            switch (userInquiryDTO.CustomActionType)
            {
                case "GROUP_ACTION":

                    switch (userInquiryDTO.CustomActionName)
                    {
                        case "delete":
                            this.AccountService.DeleteUser(userInquiryDTO.IDs);
                            break;
                        case "freeze":
                            foreach (var id in userInquiryDTO.IDs)
                            {
                                var model = this.AccountService.GetUser(id);
                                model.IsActive = false;
                                this.AccountService.SaveUser(model);
                            }
                            break;

                        case "active":
                            foreach (var id in userInquiryDTO.IDs)
                            {
                                var model = this.AccountService.GetUser(id);
                                model.IsActive = true;
                                this.AccountService.SaveUser(model);
                            }
                            break;
                    }
                    break;

                case "DELETE":
                    this.AccountService.DeleteUser(userInquiryDTO.IDs);
                    break;
            }
        


        TransactionalInformation transaction = new TransactionalInformation();
            var allUsers = this.AccountService.GetUserList(null);
            IEnumerable<User> filterUsers = allUsers;
            if (!string.IsNullOrEmpty(userInquiryDTO.LoginName))
                filterUsers = filterUsers.Where(c => c.LoginName.Contains(userInquiryDTO.LoginName));
            if (!string.IsNullOrEmpty(userInquiryDTO.Mobile))
                filterUsers = filterUsers.Where(c => c.Mobile == userInquiryDTO.Mobile);
            if(!string.IsNullOrEmpty(userInquiryDTO.Email))
                filterUsers = filterUsers.Where(c => c.Email.Contains(userInquiryDTO.Email));
            if (userInquiryDTO.IsActive!=null)
                filterUsers = filterUsers.Where(c => c.IsActive == userInquiryDTO.IsActive);


            int start = (userInquiryDTO.CurrentPageNumber - 1) * userInquiryDTO.PageSize;
            var sortDirection = userInquiryDTO.SortDirection;
            var sortExpression = userInquiryDTO.SortExpression;


           

            filterUsers = filterUsers.Skip(start).Take(userInquiryDTO.PageSize);
            UserApiModels userWebApi = new UserApiModels();
            List<UserDTO> UserList = new List<UserDTO>();

            foreach (User user in filterUsers)
            {
                UserList.Add(new UserDTO
                {
                    ID = user.ID,
                    LoginName = user.LoginName,
                    Email = user.Email,
                    Mobile = user.Mobile,
                    IsActive = user.IsActive,
                    Roles = StringUtil.CutString(string.Join(",", user.Roles.Select(r => r.Name)), 40)
                });
            };


            // Func<UserDTO, string> orderingFunction = c => "LoginName";
            Func<UserDTO, string> orderingFunction = (
                c => sortExpression.Contains("LoginName") ? c.LoginName :
                sortExpression.Contains("Email") ? c.Email :
                sortExpression.Contains("Mobile") ? c.Email :
                sortExpression.Contains("IsActive") ? c.IsActive.ToString() :
                ""

            );
            //  Func<UserDTO, string> orderingFunction = Func<UserDTO,sortExpression>;
            IEnumerable<UserDTO> Result= new List<UserDTO>();
            
            switch (sortDirection)
            {
                case "ASC":
                    
                     Result = UserList.OrderBy(orderingFunction);
                    break;

                case "DESC":
                     Result = UserList.OrderByDescending(orderingFunction);
                    break;

                default:
                    Result = UserList;
                    break;

            }

            userWebApi.Users = Result;
            userWebApi.TotalRecords = allUsers.Count();
            userWebApi.ReturnMessage = transaction.ReturnMessage;
            userWebApi.ReturnStatus = transaction.ReturnStatus;

            return Request.CreateResponse(HttpStatusCode.OK, userWebApi);
        }

        public HttpResponseMessage GetUser(int UserID)
        {
            TransactionalInformation transaction = new TransactionalInformation();
            var model = this.AccountService.GetUser(UserID);
            var roles = this.AccountService.GetRoleList();
            UserApiModels userWebApi = new UserApiModels();
            userWebApi.User = new UserDTO
            {
                LoginName = model.LoginName,
                Email = model.Email,
                Mobile = model.Mobile,
                IsActive = model.IsActive,
                Roles= StringUtil.CutString(string.Join(",", model.Roles.Select(r => r.Name)), 40)
            };

            
            userWebApi.ReturnMessage = transaction.ReturnMessage;
            userWebApi.ReturnStatus = transaction.ReturnStatus;

           // this.ViewBag.RoleIds = new SelectList(roles, "ID", "Name", string.Join(",", model.Roles.Select(r => r.ID)));
            return Request.CreateResponse(HttpStatusCode.OK, userWebApi);
        }


        [HttpPost]
        public HttpResponseMessage Post([FromBody] UserDTO userDTO)
        {
            var model = new User();
            model.Password = "111111";
            model.Password = Encrypt.MD5(model.Password);
            model.LoginName = userDTO.LoginName;
            model.Email = userDTO.Email;
            model.Mobile = userDTO.Mobile;
            model.IsActive = userDTO.IsActive;
            model.RoleIds = userDTO.IDs;
            UserApiModels userWebApi = new UserApiModels();
            TransactionalInformation transaction = new TransactionalInformation();
            try
            {
                this.AccountService.SaveUser(model);
                transaction.ReturnStatus = true;
            }
            catch(Exception ex) {
                transaction.ReturnMessage = new List<string>();
                string errorMessage = ex.Message;
                transaction.ReturnStatus = false;
                transaction.ReturnMessage.Add(errorMessage);
            }

            if (transaction.ReturnStatus == false)
            {
                userWebApi.ReturnMessage = transaction.ReturnMessage;
                userWebApi.ReturnStatus = transaction.ReturnStatus;
                userWebApi.ValidationErrors = transaction.ValidationErrors;
                var badResponse = Request.CreateResponse<UserApiModels>(HttpStatusCode.BadRequest, userWebApi);
                return badResponse;
            }

            userWebApi.IsAuthenicated = true;
            userWebApi.ReturnStatus = transaction.ReturnStatus;
            userWebApi.ReturnMessage.Add("注册成功");
           
       

            return Request.CreateResponse(HttpStatusCode.OK,userWebApi);
        }

        [HttpPatch]
        [HttpPut]
        public HttpResponseMessage Put()
        {
            return Request.CreateResponse(HttpStatusCode.OK);
        }

        public HttpResponseMessage Delete()
        {
            return Request.CreateResponse(HttpStatusCode.OK);
        }
    }
}