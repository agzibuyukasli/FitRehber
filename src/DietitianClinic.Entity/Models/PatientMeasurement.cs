using System;
using DietitianClinic.Entity.Base;

namespace DietitianClinic.Entity.Models
{
    /// <summary>
    /// Hasta ölçümleri (boy, kilo, vücut ölçüleri vb.)
    /// </summary>
    public class PatientMeasurement : BaseEntity
    {
        public int PatientId { get; set; }
        public DateTime MeasurementDate { get; set; }
        public double Weight { get; set; } // kg cinsinden
        public double Height { get; set; } // cm cinsinden
        public double? BMI { get; set; } // Vücut Kitle İndeksi (otomatik hesaplanacak)
        public double? WaistCircumference { get; set; } // Bel çevresi
        public double? HipCircumference { get; set; } // Kalça çevresi
        public double? ChestCircumference { get; set; } // Göğüs çevresi
        public double? ArmCircumference { get; set; } // Kol çevresi
        public double? ThighCircumference { get; set; } // Uyluk çevresi
        public double? BodyFatPercentage { get; set; } // Vücut yağ yüzdesi
        public string Notes { get; set; }

        // İlişkiler
        public virtual Patient Patient { get; set; }

        /// <summary>
        /// BMI'yi hesapla: Kilo(kg) / (Boy(m) ^ 2)
        /// </summary>
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
