using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using MediConnectAPI.Data;
using MediConnectAPI.DTOs;

namespace MediConnectAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VideoController : ControllerBase
{
    private readonly MediConnectContext _db;
    private readonly IConfiguration _config;

    public VideoController(MediConnectContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    // Returns a join link for a video consult for a given appointment.
    // Provider: Jitsi (link-only) by default. Only Admin, the assigned Doctor, or the Patient can get the link.
    [Authorize(Roles = "Admin,Doctor,Patient")]
    [HttpPost("appointments/{appointmentId:int}/join")]
    public async Task<ActionResult<VideoMeetingResponse>> JoinAppointment(int appointmentId)
    {
        var role = User.Claims.FirstOrDefault(c => c.Type == "role")?.Value;
        var userId = int.Parse(User.Claims.First(c => c.Type == "userId").Value);

        var appt = await _db.Appointments
            .Include(a => a.Patient).ThenInclude(p => p.User)
            .Include(a => a.Doctor).ThenInclude(d => d.User)
            .FirstOrDefaultAsync(a => a.Id == appointmentId);

        if (appt == null) return NotFound("Cita no encontrada.");

        // Authorization: Admin can always access. Doctor/Patient must be the assigned ones.
        if (role == "Doctor")
        {
            var doctorId = await _db.Doctors.Where(d => d.UserId == userId).Select(d => d.Id).FirstOrDefaultAsync();
            if (doctorId != appt.DoctorId) return Forbid("No puedes acceder a la videollamada de otra cita.");
        }
        else if (role == "Patient")
        {
            var patientId = await _db.Patients.Where(p => p.UserId == userId).Select(p => p.Id).FirstOrDefaultAsync();
            if (patientId != appt.PatientId) return Forbid("No puedes acceder a la videollamada de otra cita.");
        }

        var displayName = role switch
        {
            "Doctor" => appt.Doctor.User.FirstName + " " + appt.Doctor.User.LastName,
            "Patient" => appt.Patient.User.FirstName + " " + appt.Patient.User.LastName,
            _ => "Admin"
        };

        var provider = _config["Video:Provider"] ?? "Jitsi";

        if (provider.Equals("Jitsi", StringComparison.OrdinalIgnoreCase))
        {
            var baseUrl = _config["Video:JitsiBaseUrl"] ?? "https://meet.jit.si";
            var secret = _config["Video:RoomNameSecret"];
            var room = BuildDeterministicRoomName(appointmentId, appt.ScheduledAt, secret);

            // Jitsi allows passing display name via URL fragment
            var encodedName = Uri.EscapeDataString(displayName);
            var joinUrl = $"{baseUrl.TrimEnd('/')}/{room}#userInfo.displayName=\"{encodedName}\"";

            return Ok(new VideoMeetingResponse
            {
                Provider = "Jitsi",
                RoomName = room,
                JoinUrl = joinUrl,
                Role = role!,
                DisplayName = displayName
            });
        }

        // Placeholder for future providers (Daily, Twilio). Return 501 for now.
        return StatusCode(501, "Proveedor de video no implementado. Configure Video:Provider=Jitsi para links directos.");
    }

    private static string BuildDeterministicRoomName(int appointmentId, DateTime scheduledAt, string? secret)
    {
        var baseName = $"mediconnect-{appointmentId}-{scheduledAt:yyyyMMddHHmm}";
        if (string.IsNullOrWhiteSpace(secret)) return baseName;

        var key = Encoding.UTF8.GetBytes(secret);
        var msg = Encoding.UTF8.GetBytes(baseName);
        using var hmac = new HMACSHA256(key);
        var hash = hmac.ComputeHash(msg);
        var shortHash = Convert.ToHexString(hash).ToLowerInvariant()[..8];
        return $"{baseName}-{shortHash}";
    }
}

