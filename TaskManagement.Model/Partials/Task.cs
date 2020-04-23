using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskManagement.Model
{
    public partial class Task
    {
        public string Label
        {
            get
            {
                switch (this.StatusId)
                {
                    case (int)TaskStatuses.ToDo:return "label-info";
                    case (int)TaskStatuses.InProgress: return "label-warning";
                    case (int)TaskStatuses.Done: return "label-success";
                    default:return "";
                }
            }
        }
    }
}
