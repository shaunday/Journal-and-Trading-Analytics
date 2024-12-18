﻿using System.ComponentModel.DataAnnotations;

namespace DayJT.Journal.DataEntities.Entities
{
    public class ContentRecord(string content)
    {
        [Required]
        public string ContentValue { get; set; } = content;

        public string ChangeNote { get; set; } = string.Empty;

        [Required]
        public DateTime CreatedAt { get; } = DateTime.Now;

        [Required]
        public DataElement CellRef { get; set; } = null!; // Navigation property to the owning Cell
    }
}
