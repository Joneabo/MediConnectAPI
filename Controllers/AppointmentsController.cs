using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MediConnectAPI.Data;
using MediConnectAPI.DTOs;
using MediConnectAPI.Models;

namespace MediConnectAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AppointmentsController : ControllerBase
{
    private readonly MediConnectContext _db;
    public AppointmentsController(MediConnectContext db) => _db = db;

    // ✅ Admin ve todas, Doctor sus citas, Patient las suyas
    [Authorize(Roles = "Admin,Doctor,Patient")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AppointmentDto>>> GetAll()
    {
        var role = User.Claims.FirstOrDefault(c => c.Type == "role")?.Value;
        var userId = int.Parse(User.Claims.First(c => c.Type == "userId").Value);

        var query = _db.Appointments
            .Include(a => a.Patient).ThenInclude(p => p.User)
            .Include(a => a.Doctor).ThenInclude(d => d.User)
            .Include(a => a.Doctor.Specialty)
            .Include(a => a.Status)
            .AsQueryable();

        if (role == "Doctor")
        {
            var doctorId = await _db.Doctors.Where(d => d.UserId == userId).Select(d => d.Id).FirstOrDefaultAsync();
            query = query.Where(a => a.DoctorId == doctorId);
        }
        else if (role == "Patient")
        {
            var patientId = await _db.Patients.Where(p => p.UserId == userId).Select(p => p.Id).FirstOrDefaultAsync();
            query = query.Where(a => a.PatientId == patientId);
        }

        var appointments = await query
            .Select(a => new AppointmentDto
            {
                Id = a.Id,
                ScheduledAt = a.ScheduledAt,
                Reason = a.Reason,
                PatientId = a.PatientId,
                PatientName = a.Patient.User.FirstName + " " + a.Patient.User.LastName,
                DoctorId = a.DoctorId,
                DoctorName = a.Doctor.User.FirstName + " " + a.Doctor.User.LastName,
                Specialty = a.Doctor.Specialty.Name,
                Status = a.Status.Name
            })
            .ToListAsync();

        return Ok(appointments);
    }

    // ✅ Crear cita: Admin o Patient
    [Authorize(Roles = "Admin,Patient")]
    [HttpPost]
    public async Task<ActionResult> Create([FromBody] MediConnectAPI.DTOs.CreateAppointmentRequest req)
    {
        // Determine the patientId to use. If role is Patient, always use the caller's PatientId.
        var role = User.Claims.FirstOrDefault(c => c.Type == "role")?.Value;
        int effectivePatientId = req.PatientId;

        if (role == "Patient")
        {
            var userId = int.Parse(User.Claims.First(c => c.Type == "userId").Value);
            var selfPatientId = await _db.Patients.Where(p => p.UserId == userId).Select(p => p.Id).FirstOrDefaultAsync();
            if (selfPatientId == 0)
                return BadRequest("Tu usuario no tiene un perfil de paciente asociado.");
            effectivePatientId = selfPatientId; // ignore any client-provided patientId
        }

        // Existence checks to provide clear 400s instead of FK errors
        var patientExists = await _db.Patients.AnyAsync(p => p.Id == effectivePatientId);
        if (!patientExists) return BadRequest("Paciente no existe.");

        var doctorExists = await _db.Doctors.AnyAsync(d => d.Id == req.DoctorId);
        if (!doctorExists) return BadRequest("Doctor no existe.");

        var statusExists = await _db.AppointmentStatuses.AnyAsync(s => s.Id == req.StatusId);
        if (!statusExists) return BadRequest("Estado de cita no existe.");

        var a = new Appointment
        {
            PatientId = effectivePatientId,
            DoctorId = req.DoctorId,
            StatusId = req.StatusId,
            ScheduledAt = req.ScheduledAt,
            Reason = req.Reason
        };

        _db.Appointments.Add(a);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Cita creada correctamente.", id = a.Id });
    }

    // ✅ Actualizar: Admin o Doctor asignado
    [Authorize(Roles = "Admin,Doctor")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult> Update(int id, [FromBody] MediConnectAPI.DTOs.UpdateAppointmentRequest req)
    {
        if (id != req.Id) return BadRequest("Id no coincide.");

        var a = await _db.Appointments.FirstOrDefaultAsync(x => x.Id == id);
        if (a == null) return NotFound("Cita no encontrada.");

        var role = User.Claims.FirstOrDefault(c => c.Type == "role")?.Value;
        var userId = int.Parse(User.Claims.First(c => c.Type == "userId").Value);

        if (role == "Doctor")
        {
            var doctorId = await _db.Doctors.Where(d => d.UserId == userId).Select(d => d.Id).FirstOrDefaultAsync();
            if (doctorId != a.DoctorId) return Forbid("No puedes modificar citas de otros doctores.");
        }

        // Optional existence checks
        if (!await _db.Patients.AnyAsync(p => p.Id == req.PatientId)) return BadRequest("Paciente no existe.");
        if (!await _db.Doctors.AnyAsync(d => d.Id == req.DoctorId)) return BadRequest("Doctor no existe.");
        if (!await _db.AppointmentStatuses.AnyAsync(s => s.Id == req.StatusId)) return BadRequest("Estado de cita no existe.");

        a.PatientId = req.PatientId;
        a.DoctorId = req.DoctorId;
        a.StatusId = req.StatusId;
        a.ScheduledAt = req.ScheduledAt;
        a.Reason = req.Reason;

        await _db.SaveChangesAsync();
        return Ok("Cita actualizada correctamente.");
    }

    // ✅ Eliminar: Solo Admin
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var a = await _db.Appointments.FindAsync(id);
        if (a == null) return NotFound("Cita no encontrada.");

        _db.Appointments.Remove(a);
        await _db.SaveChangesAsync();
        return Ok("Cita eliminada correctamente.");
    }
}
