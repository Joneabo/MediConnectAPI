using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MediConnectAPI.Data;
using MediConnectAPI.DTOs;
using MediConnectAPI.Models;

namespace MediConnectAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MedicalRecordsController : ControllerBase
{
    private readonly MediConnectContext _db;
    public MedicalRecordsController(MediConnectContext db) => _db = db;

    //Solo Admin y Doctor pueden ver expedientes
    [Authorize(Roles = "Admin,Doctor")]
    [HttpGet("{patientId:int}")]
    public async Task<ActionResult<MedicalRecordDto>> GetByPatient(int patientId)
    {
        var record = await _db.MedicalRecords
            .Where(m => m.PatientId == patientId)
            .Select(m => new MedicalRecordDto
            {
                Id = m.Id,
                PatientId = m.PatientId,
                MedicalHistory = m.MedicalHistory,
                Allergies = m.Allergies,
                ChronicDiseases = m.ChronicDiseases,
                ControlledMedications = m.ControlledMedications,
                Notes = m.Notes
            })
            .FirstOrDefaultAsync();

        if (record == null) return NotFound("Expediente no encontrado.");
        return Ok(record);
    }

    // Admin crea expediente
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult> Create([FromBody] MedicalRecord record)
    {
        _db.MedicalRecords.Add(record);
        await _db.SaveChangesAsync();
        return Ok("Expediente creado correctamente.");
    }

    // Admin y Doctor pueden actualizar
    [Authorize(Roles = "Admin,Doctor")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult> Update(int id, [FromBody] MedicalRecord record)
    {
        if (id != record.Id) return BadRequest("Id no coincide.");
        _db.Entry(record).State = EntityState.Modified;
        await _db.SaveChangesAsync();
        return Ok("Expediente actualizado correctamente.");
    }

    // Listar historial clínico del expediente
    [Authorize(Roles = "Admin,Doctor,Patient")]
    [HttpGet("{recordId:int}/history")]
    public async Task<ActionResult<IEnumerable<ClinicalHistoryDto>>> GetHistory(int recordId)
    {
        var histories = await _db.ClinicalHistories
            .Where(c => c.MedicalRecordId == recordId)
            .Select(c => new ClinicalHistoryDto
            {
                Id = c.Id,
                AppointmentId = c.AppointmentId,
                RegisteredAt = c.RegisteredAt,
                Diagnosis = c.Diagnosis,
                Treatment = c.Treatment,
                Reason = c.Reason,
                Notes = c.Notes
            })
            .ToListAsync();

        return Ok(histories);
    }
}
