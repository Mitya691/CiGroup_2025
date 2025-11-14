using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesktopClient.Model
{
    public class Card
    {
        public long Id { get; set; }             
        public int TrendId { get; set; }       
        public DateTime StartTime { get; set; }    
        public DateTime EndTime { get; set; }     

        public string? SourceSilo { get; set; }  
        public string? Direction { get; set; }
        public string? TargetSilo { get; set; }

        public decimal? Weight1 { get; set; }   
        public decimal? Weight2 { get; set; }
        public decimal? TotalWeight { get; set; }

        public DateTime CreatedAt { get; set; }  // current_timestamp() по умолчанию
        public DateTime? UpdatedAt { get; set; } // может быть null
    }
}
