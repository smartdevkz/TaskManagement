using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using TaskManagement.Model;
using Microsoft.AspNet.Identity;

namespace TaskManagement.Controllers
{
    [Authorize]
    public class ProjectController : Controller
    {
        private TaskManagementDBEntities db = new TaskManagementDBEntities();

        // GET: Project
        public ActionResult Index()
        {
            IQueryable<Project> projects = null;
            if (this.User.IsInRole("Администратор"))
            {
                projects = db.Projects;
            }
            else
            {
                var userId = this.User.Identity.GetUserId();
                projects = db.ProjectUsers.Where(x => x.UserId == userId).Select(x => x.Project);
            }
            return View(projects.ToList());
        }

        // GET: Project/Details/5
        [Authorize(Roles = "Администратор")]
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Project project = db.Projects.Find(id);
            if (project == null)
            {
                return HttpNotFound();
            }
            return View(project);
        }

        // GET: Project/Create
        [Authorize(Roles = "Администратор")]
        public ActionResult Create()
        {
            return View();
        }

        // POST: Project/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize(Roles = "Администратор")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,Name")] Project project)
        {
            if (ModelState.IsValid)
            {
                project.CreateDate = DateTime.Now;
                project.UserId = this.User.Identity.GetUserId();
                db.Projects.Add(project);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.UserId = new SelectList(db.AspNetUsers, "Id", "Email", project.UserId);
            return View(project);
        }

        // GET: Project/Edit/5
        [Authorize(Roles = "Администратор")]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Project project = db.Projects.Find(id);
            if (project == null)
            {
                return HttpNotFound();
            }
            return View(project);
        }

        // POST: Project/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Администратор")]
        public ActionResult Edit([Bind(Include = "Id,Name,CreateDate")] Project project)
        {
            if (ModelState.IsValid)
            {
                var old = db.Projects.Where(x => x.Id == project.Id).FirstOrDefault();
                old.Name = project.Name;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(project);
        }

        // GET: Project/Delete/5
        [Authorize(Roles = "Администратор")]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Project project = db.Projects.Find(id);
            if (project == null)
            {
                return HttpNotFound();
            }
            return View(project);
        }

        // POST: Project/Delete/5
        [Authorize(Roles = "Администратор")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Project project = db.Projects.Find(id);
            db.Projects.Remove(project);
            db.SaveChanges();
            return RedirectToAction("Index");
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
