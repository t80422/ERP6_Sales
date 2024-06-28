using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ERP6.Models
{
    public partial class Promise_Price
    {
        public int PP_NO { get; set; }
        public DateTime? ADD_TIME { get; set; }
        public string? PART_NO { get; set; }
        public string? CO_NO { get; set; }
        public double? PRICE { get; set; }

    }
}
