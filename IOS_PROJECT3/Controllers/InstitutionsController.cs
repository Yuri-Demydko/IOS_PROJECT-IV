﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using IOS_PROJECT3.Models;
using IOS_PROJECT3.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IOS_PROJECT3.Controllers
{
    public class InstitutionsController : Controller
    {
        private DBMergedContext DBContext;
        private UserManager<EUser> UserManager;
        public InstitutionsController(DBMergedContext context, UserManager<EUser> manager)
        {
            DBContext = context;
            UserManager = manager;
        }
        public  IActionResult Index()
        {
            
            InstitutionsViewModel model = new InstitutionsViewModel()
            {
                Institutions = DBContext.Institutions.Include(m=>m.Manager).ToList(),
                
            };
            
            return View(model);
        }

        public async Task<IActionResult> Edit(string Id)
        {
            EditInstitutionViewModel model = new EditInstitutionViewModel()
            {
                AvailableManagers = await UserManager.GetUsersInRoleAsync("Manager"),
                Name = (from inst in DBContext.Institutions where inst.Id.ToString() == Id select inst.Name).FirstOrDefaultAsync().Result,
                ManagerId = (from inst in DBContext.Institutions.Include(m => m.Manager)
                             where inst.Id.ToString() == Id
                             select inst.Manager.Id).FirstOrDefaultAsync().Result,
                Idinst=Id
               // Institution= (from inst in DBContext.Institutions.Include(m => m.Manager) where inst.Id.ToString() == Id select inst).FirstOrDefault()

            };
            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> Edit(EditInstitutionViewModel model)
        {
            var manager = await UserManager.FindByIdAsync(model.ManagerId);
            string name = model.Name;
            
            if(manager != null && !String.IsNullOrWhiteSpace(name))
            {              
                var inst = (from i in DBContext.Institutions.Include(m => m.Manager)
                            where i.Id.ToString() == model.Idinst
                            select i).FirstOrDefaultAsync().Result;

                DBContext.Update(inst).Entity.Manager = manager;
                DBContext.Update(inst).Entity.Name = name;

                await DBContext.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            ModelState.AddModelError("Edit", "Error in inst edit");
            return View(model);
        }
        public async Task<IActionResult> Create()
        {
            CreateInstitutionViewModel model = new CreateInstitutionViewModel()
            {
                AvailableManagers = await UserManager.GetUsersInRoleAsync("Manager")
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string Id)
        {
            var inst = (from i in DBContext.Institutions
                        where i.Id.ToString() == Id
                        select i).FirstOrDefaultAsync().Result;
            DBContext.Remove(inst);
           // DBContext.Institutions.Find(inst).En
            await DBContext.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateInstitutionViewModel model)
        {
            
            var manager = await UserManager.FindByIdAsync(model.ManagerId);
            string name = model.Name;
            if(manager!=null && !String.IsNullOrWhiteSpace(name))
            {
                EInstitution inst = new EInstitution()
                {
                    Name = name,
                    Manager = manager

                };
                DBContext.Institutions.Add(inst);
                await DBContext.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            ModelState.AddModelError("Create", "Error in inst create");
            return View(model);
        }
    }
}