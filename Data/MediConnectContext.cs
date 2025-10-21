using Microsoft.EntityFrameworkCore;
using MediConnectAPI.Models;

namespace MediConnectAPI.Data;

public class MediConnectContext : DbContext
{
    public MediConnectContext(DbContextOptions<MediConnectContext> options)
        : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<DoctorSpecialty> Specialties => Set<DoctorSpecialty>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<AppointmentStatus> AppointmentStatuses => Set<AppointmentStatus>();
    public DbSet<MedicalRecord> MedicalRecords => Set<MedicalRecord>();
public DbSet<ClinicalHistory> ClinicalHistories => Set<ClinicalHistory>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Doctor)
            .WithMany(d => d.Appointments)
            .HasForeignKey(a => a.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Patient)
            .WithMany(p => p.Appointments)
            .HasForeignKey(a => a.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MedicalRecord>()
            .HasOne(m => m.Patient)
            .WithOne(p => p.MedicalRecord!)
            .HasForeignKey<MedicalRecord>(m => m.PatientId);

        modelBuilder.Entity<ClinicalHistory>()
            .HasOne(c => c.MedicalRecord)
            .WithMany(m => m.ClinicalHistories)
            .HasForeignKey(c => c.MedicalRecordId);

        modelBuilder.Entity<ClinicalHistory>()
            .HasOne(c => c.Appointment)
            .WithMany()
            .HasForeignKey(c => c.AppointmentId);

    }
}
