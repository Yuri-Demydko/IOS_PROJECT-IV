﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IOS_PROJECT3.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using IOS_PROJECT3.ViewModels;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System.Data.OleDb;
using System.Data;

namespace IOS_PROJECT3.Controllers
{
    public class UsersAdminController : Controller
    {

        UserManager<EUser> userManager;
        DBMergedContext database;
        IWebHostEnvironment enviroment;
        RoleManager<IdentityRole> roleManager;


        public UsersAdminController(UserManager<EUser> userManager, DBMergedContext db, IWebHostEnvironment enviroment, RoleManager<IdentityRole> roleManager)
        {
            this.userManager = userManager;
            database = db;
            this.enviroment = enviroment;
            this.roleManager = roleManager;
           
        }

        public IActionResult Index()
        {
            var model = new UsersAdminViewModel(database)
            {
                Users = userManager.Users.ToList(),
                WhyTitles = new Dictionary<EUser, string>()
            };
            return View(model);
        }


        public IActionResult MassRegistrationFails(List<string> list)
        {
            var model = new MassRegErrorViewModel()
            {
                FailedUsers = list
            };
            return View(model);
        }
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> MassRegistration(MassRegViewModel model)
        {


            List<string> FailedUsers = new List<string>();

            for (int i = 0; i < model.Count; i++)
            {
                var fio = model.FIOs[i];
                var email = model.Emails[i];
                var pass = model.Passwords[i];
                var role = model.Roles[i];
                bool isOk = true;
                string reason = "";
                if (String.IsNullOrWhiteSpace(fio) || String.IsNullOrWhiteSpace(email) || String.IsNullOrWhiteSpace(pass) || String.IsNullOrWhiteSpace(role))
                {
                    isOk = false;
                    reason = "В ФИО/Email/Пароль/Роль было передано пустое значение";
                }
                if (isOk)
                {
                    EUser user = new EUser()
                    {
                        Email = email,
                        UserName = email,
                        FIO = fio
                    };
                    var result0=await userManager.UserValidators[0].ValidateAsync(userManager,user);
                    var result01 = await userManager.PasswordValidators[0].ValidateAsync(userManager, user, pass);
                    if (!result0.Succeeded || !result01.Succeeded)
                    {
                        isOk = false;
                        if(result0.Succeeded)
                        reason = "Неверный формат пароля";
                        if (result01.Succeeded)
                            reason = "Неверный формат Email'a, либо данный Email уже занят другим пользователем";
                    }
                    if (isOk)
                    {
                        if (!await roleManager.RoleExistsAsync(role))
                        {
                            isOk = false;
                            reason = "Роли " + role + " не существует";
                        }
                        if (isOk)
                        {
                            var result = await userManager.CreateAsync(user, pass);
                            var result2 = await userManager.AddToRoleAsync(user, role);
                            if (!result.Succeeded || !result2.Succeeded)
                            {
                                isOk = false;
                                reason = "Ошибка внесения изменений в базу данных, точная причина неизвестна";
                            }
                        }
                    }
                }
                if (!isOk)
                    FailedUsers.Add(fio + " | " + email + " | " + pass + " | " + role+" - "+reason);
            }
            if(FailedUsers.Count==0)
            return RedirectToAction("Index");
            else return RedirectToAction("MassRegistrationFails",new {list=FailedUsers});

        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                EUser user = new EUser { Email = model.Email, UserName = model.Email, FIO = model.FIO };
                var result = await userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    return RedirectToAction("Index");
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
            }
            return View(model);
        }

        public async Task<IActionResult> Edit(string id)
        {
            EUser user = await userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            EditUserViewModel model = new EditUserViewModel { Id = user.Id, Email = user.Email, FIO = user.FIO };
            return View(model);
        }
        public async Task<IActionResult> ResetPassword(string Id)
        {
            EUser user = await userManager.FindByIdAsync(Id);
            if (user == null)
            {
                return NotFound();
            }

            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var result = await userManager.ResetPasswordAsync(user, token, "Default@Pass123");
            if (result.Succeeded)
            {
                await userManager.ResetAccessFailedCountAsync(user);
                return RedirectToAction("Index");
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View();

        }

        [HttpPost]
        public async Task<IActionResult> Edit(EditUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                EUser user = await userManager.FindByIdAsync(model.Id);
                if (user != null)
                {
                    user.Email = model.Email;
                    user.UserName = model.Email;
                    user.FIO = model.FIO;//потом можно разделить
                    var result = await userManager.UpdateAsync(user);
                    if (result.Succeeded)
                    {
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                    }
                }
            }
            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            EUser user = await userManager.FindByIdAsync(id);
            if (user != null)
            {
                try
                {
                    IdentityResult result = await userManager.DeleteAsync(user);
                }
                catch (Exception ex)
                {
                    // DeleteErrorMessage = "Ошибка блять ебаный тебя рот долбоеб тупой блять";

                    return RedirectToAction("Index");
                }

            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> AddFile(IFormFile uploadedFile)
        {
            try
            {
                if (uploadedFile != null)
                {
                    // путь к папке Files
                    string path = "/RegFiles/" + uploadedFile.FileName;
                    if (!path.EndsWith(".xlsx"))
                        throw new Exception("File is not .xlsx file");
                    // сохраняем файл в папку Files в каталоге wwwroot
                    using (var fileStream = new FileStream(enviroment.WebRootPath + path, FileMode.Create))
                    {
                        await uploadedFile.CopyToAsync(fileStream);
                    }
                    // RegFile filereg = new RegFile { Name = uploadedFile.FileName, Path = enviroment.WebRootPath + path };
                    return RedirectToAction("MassRegistration", new { Path = (enviroment.WebRootPath + path) });
                }
            }
            catch (Exception e)
            {
                return RedirectToAction("ErrorLoadingFile", new { message = e.Message });
            }
            return RedirectToAction("Index");
            
        }
        public IActionResult ErrorLoadingFile(string message)
        {
            ViewBag.Message = message;
            return View();
        }
        public List<string> ReadColumn(string path, int column)
        {
            
            OleDbConnection conn = new OleDbConnection();
            //conn.ConnectionString = conn.ConnectionString = @"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" +path
            // +";Extended Properties='Excel 12.0 Xml;HDR=NO;IMEX=1;MAXSCANROWS=0'";
            conn.ConnectionString = String.Format(@"Provider=Microsoft.ACE.OLEDB.12.0;Excel 12.0 Xml;HDR=No;Data Source={0}", path);
            conn.Open();
            var dtSchema = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, new object[] { null, null, null, "TABLE" });
            var Sheet1 = dtSchema.Rows[0].Field<string>("TABLE_NAME");
            var comm = new OleDbCommand();
            comm.Connection = conn;
            comm.CommandText = String.Format("Select * from [{0}]",Sheet1);
             OleDbDataAdapter adapter = new OleDbDataAdapter(comm.CommandText, conn);
            DataTable dsXLS = new DataTable();
            adapter.Fill(dsXLS);
            List<string> result = new List<string>();

            var foo = dsXLS.Rows;
            for (int i = 0; i < foo.Count; i++)
            {
                result.Add(foo[i].ItemArray[column].ToString());
            }
            return result;
        }
        public IActionResult MassRegistration(string Path)
        {
            try
            {
                MassRegViewModel model = new MassRegViewModel()
                {
                    FIOs = ReadColumn(Path, 0),
                    Emails = ReadColumn(Path, 1),
                    Passwords = ReadColumn(Path, 2),
                    Roles = ReadColumn(Path, 3)
                };
                int em = model.Emails.Count;
                int fi = model.FIOs.Count;
                int pa = model.FIOs.Count;
                int ro = model.Roles.Count;
                model.Count = Math.Max(Math.Max(em, fi), Math.Max(pa, ro));
                return View(model);
            }
            catch(Exception e)
            {
                return RedirectToAction("ErrorLoadingFile",new {message=e.Message });
            }
                            
        }
    }
}