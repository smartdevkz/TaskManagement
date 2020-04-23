using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using TaskManagement.Model;

namespace TaskManagement.Controllers
{
    [Authorize(Roles = "Администратор")]
    public class ProjectUserController : Controller
    {
        private TaskManagementDBEntities db = new TaskManagementDBEntities();

        private int projectId
        {
            get
            {
                return TempData["ProjectId"] != null ? Convert.ToInt32(TempData["ProjectId"]) : 0;
            }
            set
            {
                TempData["ProjectId"] = value;
            }
        }
        // GET: ProjectUser
        public ActionResult Index(int projectId)
        {
            this.projectId = projectId;
            var projectUsers = db.ProjectUsers.Where(x => x.ProjectId == projectId).Include(p => p.AspNetUser).Include(p => p.Project);
            return View(projectUsers.ToList());
        }

        // GET: ProjectUser/Create
        public ActionResult Create()
        {
            var projectUserIds = db.ProjectUsers.Where(x => x.ProjectId == projectId).Select(x => x.UserId);
            ViewBag.UserId = new SelectList(db.AspNetUsers.Where(x => !projectUserIds.Contains(x.Id)), "Id", "Email");

            var role = db.AspNetRoles.FirstOrDefault(x => x.Id == "2");
            var roles = new List<SelectListItem>();
            roles.Add(new SelectListItem() { Value = role.Id, Text = role.Name });
            roles.Add(new SelectListItem() { Value = "", Text = "Не выбрано" });
            ViewBag.RoleId = roles;

            return View();
        }

        // POST: ProjectUser/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,ProjectId,RoleId, UserId,CreateDate")] ProjectUser projectUser)
        {
            if (ModelState.IsValid)
            {
                if (string.IsNullOrEmpty(projectUser.RoleId)) projectUser.RoleId = null;
                projectUser.CreateDate = DateTime.Now;
                db.ProjectUsers.Add(projectUser);
                db.SaveChanges();
                return RedirectToAction("Index", new { projectId = projectUser.ProjectId });
            }

            ViewBag.UserId = new SelectList(db.AspNetUsers, "Id", "Email", projectUser.UserId);
            return View(projectUser);
        }

        // GET: ProjectUser/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ProjectUser projectUser = db.ProjectUsers.Find(id);
            if (projectUser == null)
            {
                return HttpNotFound();
            }
            return View(projectUser);
        }

        // POST: ProjectUser/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            ProjectUser projectUser = db.ProjectUsers.Find(id);
            db.ProjectUsers.Remove(projectUser);
            db.SaveChanges();
            return RedirectToAction("Index", new { projectId = projectUser.ProjectId });
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
