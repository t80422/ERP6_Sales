﻿using System;
using System.Collections.Generic;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace ERP6.Models
{
    public partial class SyncLogHistory
    {
        public string ApsrvId { get; set; }
        public string Lastimportdatetime { get; set; }
        public string Lastexportdatetime { get; set; }
        public string Ip { get; set; }
        public string SyncType { get; set; }
    }
}
