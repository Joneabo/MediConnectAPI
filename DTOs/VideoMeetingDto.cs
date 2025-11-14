namespace MediConnectAPI.DTOs;

public class VideoMeetingResponse
{
    public string Provider { get; set; } = null!; // e.g., Jitsi, Daily, Twilio
    public string RoomName { get; set; } = null!;
    public string JoinUrl { get; set; } = null!;
    public string Role { get; set; } = null!; // Admin | Doctor | Patient
    public string DisplayName { get; set; } = null!;
}

