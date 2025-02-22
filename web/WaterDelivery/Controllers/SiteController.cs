﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using WaterDelivery.Controllers.Api;
using WaterDelivery.Models;
using WaterDelivery.Models.TableDb;
using WaterDelivery.ViewModels;

namespace WaterDelivery.Controllers
{
    
    public class SiteController : BaseController
    {

        private ApplicationDbContext db = new ApplicationDbContext();


       

        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Login(string Phone, string Password)
        {
            var checkuser = db.Client.Where(x => x.phone == Phone && x.password == Password).FirstOrDefault();
            if (checkuser != null)
            {
                if (checkuser.active == false && checkuser.active_code == true)
                {
                    return Json(new { key = 0, msg = "This account is locked by the admin" }, JsonRequestBehavior.AllowGet);
                }
                else if (checkuser.active_code == false)
                {
                    DateTime now = DateTime.Now;
                    HttpCookie userInfo = new HttpCookie("userInfo");
                    userInfo["UserName"] = checkuser.user_name;
                    string UserId = encrypt(checkuser.id.ToString());
                    userInfo["UserId"] = UserId;
                    userInfo.Expires = now.AddMonths(2);
                    Response.Cookies.Add(userInfo);

                    return Json(new { key = 2, msg = "This account has not yet been activated" }, JsonRequestBehavior.AllowGet);
                }
                else
                { 
                    DateTime now = DateTime.Now;
                    HttpCookie userInfo = new HttpCookie("userInfo");
                    userInfo["UserName"] = checkuser.user_name;
                    string UserId = encrypt(checkuser.id.ToString());
                    userInfo["UserId"] = UserId;
                    //userInfo["UserId"] = checkuser.id.ToString();
                    userInfo.Expires = now.AddMonths(2);
                    Response.Cookies.Add(userInfo);

                    return Json(new { key = 1, msg = "successfully registered.." }, JsonRequestBehavior.AllowGet);
                   
                }
            }
            else
            {
                return Json(new { key = 0, msg = "Please check the data" }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public ActionResult Register()
        {
            ViewBag.Cities = GetCitiess();
            return View();
        }

        [HttpPost]
        public ActionResult Register(UserViewModel model)
        {

            var phone = (from st in db.Client where st.phone == model.Phone select st).FirstOrDefault();
            var phone_provider = (from st in db.Provider where st.phone == model.Phone select st).FirstOrDefault();

            if (phone != null || phone_provider != null)
            {
                return Json(new { key = 0, msg = "Sorry, this phone already exists" }, JsonRequestBehavior.AllowGet);
            }
            var ChkName = (from st in db.Client where st.user_name == model.Name select st).FirstOrDefault();

            if (ChkName != null)
            {
                return Json(new { key = 0, msg = "Sorry, this name already exists" }, JsonRequestBehavior.AllowGet);
            }

            Client newclient = new Client
            {
                user_name = model.Name,
                phone = model.Phone,
                img = BaisUrlHoste + "Content/Img/User/generic-user.png",
                active = true,
                password = model.Password,
                active_code = false,
                date = DateTime.Now,
                fk_cityID = model.CityId,
                code = GetFormNumber()
            };
            db.Client.Add(newclient);
            db.SaveChanges();

            DateTime now = DateTime.Now;
            HttpCookie userInfo = new HttpCookie("userInfo");
            userInfo["UserName"] = newclient.user_name;
            string UserId = encrypt(newclient.id.ToString());
            userInfo["UserId"] = UserId;
            //userInfo["UserId"] = newclient.id.ToString();
            userInfo.Expires = now.AddMonths(2);
            Response.Cookies.Add(userInfo);

            //Send Massage to phone
            string s = SendMessageText("Validation code ", newclient.phone, newclient.code);

            return Json(new { key = 1, msg = "successfully registered.." }, JsonRequestBehavior.AllowGet);

        }


        [HttpGet]
        public ActionResult ActivePhone()
        {
            return View();
        }


        [HttpGet]
        public ActionResult ConfirmCode()
        {
            return View();
        }

        public ActionResult ChkConfirmCode(string Code)
        {
            int UserId = GetUserId();

            var ChkCode = db.Client.Where(x => x.id == UserId && x.code == Code).FirstOrDefault();
            if (ChkCode != null)
            {
                ChkCode.active_code = true;
                db.SaveChanges();
                return Json(new { key = 1, msg = "Activated successfully.." }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new { key = 0, msg = "Please check the code.." }, JsonRequestBehavior.AllowGet);
            }
        }



        public ActionResult ForgetPassword()
        {
            return View();
        }

        public ActionResult SendCode(string Phone)
        {
            var ChkPhone = db.Client.Where(x => x.phone == Phone).FirstOrDefault();
            if (ChkPhone != null)
            {
                ChkPhone.code = GetFormNumber();
                db.SaveChanges();

                DateTime now = DateTime.Now;
                HttpCookie userInfo = new HttpCookie("userInfo");
                userInfo["UserName"] = ChkPhone.user_name;
                //userInfo["UserId"] = ChkPhone.id.ToString();
                string UserId = encrypt(ChkPhone.id.ToString());
                userInfo["UserId"] = UserId;
                userInfo.Expires = now.AddMinutes(20);
                Response.Cookies.Add(userInfo);

                //Send Massage to phone
                string s = SendMessageText("Validation code ", ChkPhone.phone, ChkPhone.code);
                return Json(new { key = 1, msg = "Verification code has been sent successfully.." }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new { key = 0, msg = "Please check the phone number.." }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public ActionResult ForgetPasswordCode()
        {
            return View();
        }

        [HttpPost]
        public ActionResult ForgetPasswordCode(string Code)
        {

            int UserId = GetUserId();
            var ChkCode = db.Client.Where(x => x.id == UserId && x.code == Code).FirstOrDefault();

            if (ChkCode != null)
            {
                return Json(new { key = 1, msg = "Code Verified .." }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new { key = 0, msg = "Please check the code .." }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public ActionResult ResetPassword()
        {
            return View();
        }

        [HttpPost]
        public ActionResult ResetPassword(string newpassword)
        {
            int UserId = GetUserId();
            if (UserId != 0)
            {
                var ClientDB = db.Client.Where(x => x.id == UserId).FirstOrDefault();
                ClientDB.password = newpassword;
                db.SaveChanges();

                if (Request.Cookies["userInfo"] != null)
                {
                    Response.Cookies["userInfo"].Expires = DateTime.Now.AddDays(-1);
                }

                return Json(new { key = 1, msg = "Password changed .." }, JsonRequestBehavior.AllowGet);

            }
            else
            {
                return Json(new { key = 0, msg = "Something went wrong .." }, JsonRequestBehavior.AllowGet);
            }
        }

       

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

    }
}