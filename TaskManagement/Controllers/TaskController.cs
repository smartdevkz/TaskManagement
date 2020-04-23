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
    public class TaskController : Controller
    {
        private TaskManagementDBEntities db = new TaskManagementDBEntities();

        // GET: Task
        public ActionResult Index(int projectId = 0, string orderBy = "", int status = 2)
        {
            var userId = this.User.Identity.GetUserId();
            ViewBag.ProjectId = projectId;
            ViewBag.StatusId = status;
            ViewBag.CanCRUD = CanCRUD(projectId);

            var tasks = db.Tasks.Include(t => t.Project).Include(t => t.TaskStatus);
            if (projectId > 0) tasks = tasks.Where(x => x.ProjectId == projectId);

            var dtNow = DateTime.Now.Date;

            if (!this.User.IsInRole("Администратор"))
            {
                var userProjects = db.ProjectUsers.Where(x => x.UserId == userId && x.RoleId == "2").Select(x => x.ProjectId);
                if (userProjects.Any())
                {
                    tasks = tasks.Where(x => userProjects.Contains(x.ProjectId));
                }
                else
                {
                    tasks = tasks.Where(x => x.ExecutorId == userId);
                }
            }

            if (!string.IsNullOrEmpty(orderBy))
            {
                switch (orderBy.ToLower())
                {
                    case "project": tasks = tasks.OrderBy(x => x.ProjectId); break;
                    case "user": tasks = tasks.OrderBy(x => x.ExecutorId); break;
                    case "status": tasks = tasks.OrderBy(x => x.StatusId); break;
                    default: break;
                }
            }

            switch (status)
            {
                case (int)TaskStatuses.ToDo: tasks = tasks.Where(x => x.StatusId == status && x.StartDate > dtNow); break;
                case (int)TaskStatuses.InProgress: tasks = tasks.Where(x => x.StatusId == (int)TaskStatuses.InProgress || (x.StatusId == (int)TaskStatuses.ToDo && x.StartDate <= dtNow && x.EndDate >= dtNow)); break;
                case (int)TaskStatuses.Done: tasks = tasks.Where(x => x.StatusId == status); break;
                case (int)TaskStatuses.All: break;
                case (int)TaskStatuses.NextWeek:
                    var nextWeekStart = GetNextWeekday(DateTime.Today, DayOfWeek.Monday);
                    var nextWeekEnd = GetNextWeekday(DateTime.Today, DayOfWeek.Sunday);
                    tasks = tasks.Where(x => x.StatusId == (int)TaskStatuses.ToDo && x.StartDate >= nextWeekStart && x.StartDate <= nextWeekEnd);
                    break;
                default: break;
            }
            return View(tasks.ToList());
        }

        private DateTime GetNextWeekday(DateTime start, DayOfWeek day)
        {
            int daysToAdd = ((int)day - (int)start.DayOfWeek + 7) % 7;
            return start.AddDays(daysToAdd);
        }

        private DateTime GetCurrentWeekday(DateTime start, DayOfWeek day)
        {
            int daysToAdd = ((int)day - (int)start.DayOfWeek) % 7;
            return start.AddDays(daysToAdd);
        }

        private bool CanCRUD(int projectId)
        {
            if (this.User.IsInRole("Администратор")) return true;
            var userId = this.User.Identity.GetUserId();
            var userProjects = db.ProjectUsers.Where(x => x.UserId == userId && x.RoleId == "2").Select(x => x.ProjectId);
            if (userProjects.Any())
            {
                return userProjects.Contains(projectId);
            }
            return false;
        }

        // GET: Task/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Task task = db.Tasks.Find(id);
            if (task == null)
            {
                return HttpNotFound();
            }
            return View(task);
        }

        [HttpPost]
        public ActionResult Details(Task task)
        {
            Task old = db.Tasks.Find(task.Id);
            if (task == null)
            {
                return HttpNotFound();
            }
            old.StatusId = task.StatusId;

            var log = new TaskStatusLog();
            log.TaskId = task.Id;
            log.StatusId = task.StatusId;
            log.CreateDate = DateTime.Now;
            db.TaskStatusLogs.Add(log);

            db.SaveChanges();

            return View(task);
        }

        // GET: Task/Create
        public ActionResult Create(int projectId)
        {
            if (!CanCRUD(projectId)) throw new Exception("У вас нет разрешения на эту страницу");
            ViewBag.ProjectId = projectId;
            ViewBag.ExecutorId = new SelectList(db.ProjectUsers.Where(x => x.ProjectId == projectId).Select(x => x.AspNetUser), "Id", "Email");
            ViewBag.StatusId = new SelectList(db.TaskStatuses, "Id", "Name");
            return View();
        }

        // POST: Task/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,Title,Description,ProjectId,ExecutorId,StartDate,EndDate,StatusId")] Task task)
        {
            if (!CanCRUD(task.ProjectId)) throw new Exception("У вас нет разрешения на эту страницу");
            if (ModelState.IsValid)
            {
                task.CreatorId = this.User.Identity.GetUserId();
                task.CreateDate = DateTime.Now;
                db.Tasks.Add(task);
                db.SaveChanges();
                return RedirectToAction("Index", new { projectId = task.ProjectId });
            }

            ViewBag.ExecutorId = new SelectList(db.AspNetUsers, "Id", "Email", task.ExecutorId);
            ViewBag.StatusId = new SelectList(db.TaskStatuses, "Id", "Name", task.StatusId);
            return View(task);
        }

        // GET: Task/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Task task = db.Tasks.Find(id);
            if (!CanCRUD(task.ProjectId)) throw new Exception("У вас нет разрешения на эту страницу");
            if (task == null)
            {
                return HttpNotFound();
            }

            ViewBag.ExecutorId = new SelectList(db.AspNetUsers, "Id", "Email", task.ExecutorId);
            ViewBag.StatusId = new SelectList(db.TaskStatuses, "Id", "Name", task.StatusId);
            return View(task);
        }

        // POST: Task/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,Title,Description,ProjectId,ExecutorId,StartDate,EndDate,StatusId,CreateDate")] Task task)
        {
            if (!CanCRUD(task.ProjectId)) throw new Exception("У вас нет разрешения на эту страницу");
            if (ModelState.IsValid)
            {
                var old = db.Tasks.Where(x => x.Id == task.Id).FirstOrDefault();
                old.ModifiedDate = DateTime.Now;
                old.Title = task.Title;
                old.Description = task.Description;
                old.StartDate = task.StartDate;
                old.EndDate = task.EndDate;
                old.ExecutorId = task.ExecutorId;
                if(old.StatusId!= task.StatusId)
                {
                    var log = new TaskStatusLog();
                    log.TaskId = old.Id;
                    log.CreateDate = DateTime.Now;
                    log.StatusId = task.StatusId;
                    db.TaskStatusLogs.Add(log);
                }

                old.StatusId = task.StatusId;
                db.SaveChanges();
                return RedirectToAction("Index", new { projectId = task.ProjectId });
            }

            ViewBag.ExecutorId = new SelectList(db.AspNetUsers, "Id", "Email", task.ExecutorId);
            ViewBag.StatusId = new SelectList(db.TaskStatuses, "Id", "Name", task.StatusId);
            return View(task);
        }

        // GET: Task/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Task task = db.Tasks.Find(id);
            if (!CanCRUD(task.ProjectId)) throw new Exception("У вас нет разрешения на эту страницу");
            if (task == null)
            {
                return HttpNotFound();
            }
            return View(task);
        }

        // POST: Task/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Task task = db.Tasks.Find(id);
            if (!CanCRUD(task.ProjectId)) throw new Exception("У вас нет разрешения на эту страницу");
            db.Tasks.Remove(task);
            db.SaveChanges();
            return RedirectToAction("Index", new { projectId = task.ProjectId });
        }

        [ChildActionOnly]
        public ActionResult Statistics()
        {
            var monday = (DateTime.Now.DayOfWeek != DayOfWeek.Monday) ? GetCurrentWeekday(DateTime.Now, DayOfWeek.Monday) : DateTime.Now;
            var sunday = (DateTime.Now.DayOfWeek != DayOfWeek.Sunday) ? GetNextWeekday(DateTime.Now, DayOfWeek.Sunday) : DateTime.Now;

            int doneTasksCount = 0;
            int reopenedTasksCount = 0;

            var taskIds = db.TaskStatusLogs.Where(x => x.CreateDate >= monday && x.CreateDate <= sunday).Select(x => x.TaskId);
            var logs = db.TaskStatusLogs.Where(x => taskIds.Contains(x.TaskId)).GroupBy(x => x.TaskId).Select(x => new { x.Key, Items = x.OrderByDescending(c => c.CreateDate) });

            foreach (var i in logs)
            {
                if (i.Items.First().StatusId == (int)TaskStatuses.Done)
                {
                    doneTasksCount++;
                    continue;
                }
                else if (i.Items.First().StatusId == (int)TaskStatuses.ToDo || i.Items.First().StatusId == (int)TaskStatuses.InProgress)
                {
                    if (i.Items.Where(x => x.StatusId == (int)TaskStatuses.Done).Any())
                    {
                        reopenedTasksCount++;
                        continue;
                    }
                }
            }

            ViewBag.DoneTasksCount = doneTasksCount;
            ViewBag.ReopenedTasksCount = reopenedTasksCount;

            return PartialView("_Statistics");
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
