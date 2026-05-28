namespace PalletSignalRHub.Models
{
    public class DeviceConnection
    {
        public string DeviceId { get; set; } = string.Empty;
        public string ConnectionId { get; set; } = string.Empty;
        public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;
        public string DeviceType { get; set; } = "Unknown"; // "Desktop" o "Mobile"  
    }
}