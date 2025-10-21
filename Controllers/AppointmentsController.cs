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
    public async Task<ActionResult> Create([FromBody] Appointment a)
    {
        _db.Appointments.Add(a);
        await _db.SaveChangesAsync();
        return Ok("Cita creada correctamente.");
    }

    // ✅ Actualizar: Admin o Doctor asignado
    [Authorize(Roles = "Admin,Doctor")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult> Update(int id, [FromBody] Appointment a)
    {
        if (id != a.Id) return BadRequest("Id no coincide.");

        var role = User.Claims.FirstOrDefault(c => c.Type == "role")?.Value;
        var userId = int.Parse(User.Claims.First(c => c.Type == "userId").Value);

        if (role == "Doctor")
        {
            var doctorId = await _db.Doctors.Where(d => d.UserId == userId).Select(d => d.Id).FirstOrDefaultAsync();
            if (doctorId != a.DoctorId) return Forbid("No puedes modificar citas de otros doctores.");
        }

        _db.Entry(a).State = EntityState.Modified;
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
