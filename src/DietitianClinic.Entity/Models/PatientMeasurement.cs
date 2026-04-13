using System;
using DietitianClinic.Entity.Base;

namespace DietitianClinic.Entity.Models
{
    public class PatientMeasurement : BaseEntity
    {
        public int PatientId { get; set; }
        public DateTime MeasurementDate { get; set; }
        public double Weight { get; set; }
        public double Height { get; set; }
        public double? BMI { get; set; }
        public double? WaistCircumference { get; set; }
        public double? HipCircumference { get; set; }
        public double? ChestCircumference { get; set; }
        public double? ArmCircumference { get; set; }
        public double? ThighCircumference { get; set; }
        public double? BodyFatPercentage { get; set; }
        public string Notes { get; set; }

        public virtual Patient Patient { get; set; }

        public void CalculateBMI()
        {
            if (Weight > 0 && Height > 0)
            {
                double heightInMeters = Height / 100;
                BMI = Math.Round(Weight / (heightInMeters * heightInMeters), 2);
            }
        }
    }
}
