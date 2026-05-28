using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace PalletSignalRHub.Hubs
{
    public class PalletHub : Hub
    {
        private readonly ILogger<PalletHub> _logger;
        private static readonly ConcurrentDictionary<string, string> _deviceConnections = new();

        private static readonly ConcurrentDictionary<string, string> _clientTrips = new();
        private static readonly ConcurrentDictionary<string, bool> _globalTripsInUse = new();
        public PalletHub(ILogger<PalletHub> logger)
        {
            _logger = logger;
        }
        /// <summary>
        /// Health check - responde al ping del cliente para verificar conectividad
        /// </summary>
        public async Task Ping()
        {
            await Clients.Caller.SendAsync("Pong");
        }

        public async Task SendPalletListToMobile(string deviceId, object palletsData)
        {
            if (_deviceConnections.TryGetValue(deviceId, out string? connectionId))
            {
                await Clients.Client(connectionId).SendAsync("PalletListUpdated", palletsData);
                _logger.LogInformation("📋 Lista de pallets enviada al móvil - Device: {DeviceId}", deviceId);
            }
            else
            {
                _logger.LogWarning("⚠️ Dispositivo no encontrado para envío de lista: {DeviceId}", deviceId);
            }
        }
        public async Task NotifyTripReopened(string tripId)
        {
            _logger.LogInformation("🔄 Notificando viaje reabierto: {TripId}", tripId);
            await Clients.All.SendAsync("TripReopened", tripId);
        }
        // AGREGAR al PalletHub.cs  
        public async Task SendActiveTripWithPalletsToMobile(string deviceId, object tripData, object palletsData)
        {
            if (_deviceConnections.TryGetValue(deviceId, out string? connectionId))
            {
                await Clients.Client(connectionId).SendAsync("ActiveTripWithPallets", tripData, palletsData);
                _logger.LogInformation("📤 Viaje activo con pallets enviado al móvil - Device: {DeviceId}", deviceId);
            }
        }
        // Métodos que la aplicación de escritorio ya espera  
        public async Task NotifyActiveTrip(string tripId, object tripData)
        {
            _logger.LogInformation("🔄 Notificando viaje activo: {TripId}", tripId);
            await Clients.All.SendAsync("ActiveTripChanged", tripId, tripData);
        }
        
        public async Task NotifyTripCreated(object tripData)
        {
            _logger.LogInformation("🆕 Notificando nuevo viaje creado");
            await Clients.All.SendAsync("NewTripCreated", tripData);
        }
        
        public async Task NotifyTripFinalized(string tripId)
        {
            _logger.LogInformation("🏁 Notificando finalización de viaje: {TripId}", tripId);
            await Clients.All.SendAsync("TripFinalized", tripId);
        }
        // Método para eliminar pallet desde móvil
        public async Task DeletePalletFromMobile(string tripId, string palletNumber, string deviceId)
        {
            _logger.LogInformation("🗑️ Solicitud de eliminación recibida desde móvil - Pallet: {PalletNumber}, Device: {DeviceId}",
                                  palletNumber, deviceId);

            // Reenviar la solicitud a la aplicación de escritorio  
            await Clients.All.SendAsync("PalletDeleteRequested", tripId, palletNumber, deviceId);
        }
        // Método para enviar confirmación de éxito al móvil
        public async Task SendPalletSuccessToMobile(string tripId, string successMessage, string deviceId)
        {
            _logger.LogInformation("✅ Enviando confirmación de éxito al móvil - Device: {DeviceId}", deviceId);

            // Enviar confirmación específicamente al dispositivo que hizo la solicitud  
            await Clients.All.SendAsync("PalletOperationSuccess", tripId, successMessage, deviceId);
        }

        // Gestión de grupos por viaje  
        public async Task JoinTripGroup(string tripId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Trip_{tripId}");
            _logger.LogInformation("👥 Cliente {ConnectionId} unido al grupo del viaje: {TripId}",
                                 Context.ConnectionId, tripId);
        }

        public async Task LeaveTripGroup(string tripId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Trip_{tripId}");
            _logger.LogInformation("👋 Cliente {ConnectionId} salió del grupo del viaje: {TripId}",
                                 Context.ConnectionId, tripId);
        }

        // Métodos para dispositivos móviles  
        public async Task RequestActiveTrip(string deviceId)
        {
            _deviceConnections[deviceId] = Context.ConnectionId;
            _logger.LogInformation("📱 Solicitud de viaje activo desde: {DeviceId}", deviceId);
            await Clients.All.SendAsync("ActiveTripRequested", deviceId);
        }

        public async Task SendPalletNumber(string palletNumber, string deviceId)
        {
            _deviceConnections[deviceId] = Context.ConnectionId;
            _logger.LogInformation("📱 Número de pallet recibido desde móvil: {PalletNumber}, Device: {DeviceId}",
                                 palletNumber, deviceId);
            await Clients.All.SendAsync("PalletNumberReceived", palletNumber, deviceId);
        }

        // NUEVO: Método para pallets con ediciones  
        public async Task SendPalletWithEdits(string palletNumber, object editedData, string deviceId)
        {
            _deviceConnections[deviceId] = Context.ConnectionId;
            _logger.LogInformation("📝 Pallet con ediciones recibido: {PalletNumber}, Device: {DeviceId}",
                                 palletNumber, deviceId);

            // CAMBIO CLAVE: Enviar evento diferente para ediciones  
            await Clients.All.SendAsync("PalletEditReceived", palletNumber, editedData, deviceId);
        }

        // Métodos para enviar respuestas al móvil  
        public async Task SendPalletProcessedToMobile(string tripId, object pallet, string deviceId)
        {
            if (_deviceConnections.TryGetValue(deviceId, out string? connectionId))
            {
                await Clients.Client(connectionId).SendAsync("PalletProcessed", tripId, pallet, deviceId);
                _logger.LogInformation("✅ Pallet procesado enviado al móvil - Trip: {TripId}, Device: {DeviceId}",
                                     tripId, deviceId);
            }
        }
        // NUEVO: Método específico para pallets bicolor E50G6CB  
        public async Task SendBicolorPalletProcessedToMobile(string tripId, object bicolorPallet, string deviceId)
        {
            if (_deviceConnections.TryGetValue(deviceId, out string? connectionId))
            {
                await Clients.Client(connectionId).SendAsync("BicolorPalletProcessed", tripId, bicolorPallet, deviceId);
                _logger.LogInformation("🎨 Pallet bicolor procesado enviado al móvil - Trip: {TripId}, Device: {DeviceId}",
                     tripId, deviceId);
            }
            else
            {
                _logger.LogWarning("⚠️ Dispositivo no encontrado para pallet bicolor: {DeviceId}", deviceId);
            }
        }
        public async Task SendPalletErrorToMobile(string tripId, string errorMessage, string deviceId)
        {
            if (_deviceConnections.TryGetValue(deviceId, out string? connectionId))
            {
                await Clients.Client(connectionId).SendAsync("PalletError", errorMessage, deviceId);
                _logger.LogInformation("❌ Error enviado al móvil - Trip: {TripId}, Device: {DeviceId}",
                                     tripId, deviceId);
            }
        }

        public async Task SendActiveTripInfoToMobile(string deviceId, object tripData, object palletsData)
        {
            if (_deviceConnections.TryGetValue(deviceId, out string? connectionId))
            {
                await Clients.Client(connectionId).SendAsync("ActiveTripInfo", tripData, palletsData);
                _logger.LogInformation("📤 Información del viaje activo enviada al móvil - Device: {DeviceId}", deviceId);
            }
        }

        // Gestión de conexiones  
        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation("🔗 Cliente conectado: {ConnectionId}", Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            // Limpiar conexiones de dispositivos móviles    
            var deviceToRemove = _deviceConnections.FirstOrDefault(x => x.Value == Context.ConnectionId);
            if (!deviceToRemove.Equals(default(KeyValuePair<string, string>)))
            {
                _deviceConnections.TryRemove(deviceToRemove.Key, out _);
            }

            // MEJORADO: Limpiar viajes en uso cuando un cliente se desconecta    
            if (_clientTrips.TryRemove(Context.ConnectionId, out string? numeroGuia))
            {
                // NUEVO: Actualizar estado global  
                _globalTripsInUse[numeroGuia] = false;

                _logger.LogInformation("🧹 Liberando viaje {NumeroGuia} por desconexión de cliente {ConnectionId}",
                                     numeroGuia, Context.ConnectionId);

                // Notificar a otros clientes que el viaje está libre    
                await Clients.Others.SendAsync("TripInUseChanged", numeroGuia, false);
                _logger.LogInformation("📤 Notificación de liberación enviada a otros clientes");
            }

            _logger.LogInformation("🔌 Cliente desconectado: {ConnectionId}", Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }

        // NUEVO: Método para notificar desconexión de cliente  
        public async Task NotifyClientDisconnected(string connectionId)
        {
            _logger.LogInformation("🧹 Limpiando viajes en uso para cliente desconectado: {ConnectionId}", connectionId);
            await Clients.Others.SendAsync("ClientDisconnected", connectionId);
        }

        public async Task SendNoActiveTripToMobile(string message, string deviceId)
        {
            if (_deviceConnections.TryGetValue(deviceId, out string? connectionId))
            {
                await Clients.Client(connectionId).SendAsync("NoActiveTripAvailable", message);
                _logger.LogInformation("⚠️ Notificación de ausencia de viaje activo enviada - Device: {DeviceId}", deviceId);
            }
        }
          // Método para notificar lista de pallets actualizada a todos en el grupo del viaje
        public async Task BroadcastPalletListUpdate(string tripId, object palletsData)
        {
            _logger.LogInformation("📤 Broadcasting lista de pallets actualizada para viaje: {TripId}", tripId);
            await Clients.Group($"Trip_{tripId}").SendAsync("PalletListUpdated", palletsData);
        }

        // Método para enviar lista de variedades al dispositivo móvil  
        public async Task SendVariedadesToMobile(string deviceId)
        {
            _deviceConnections[deviceId] = Context.ConnectionId;
            _logger.LogInformation("📱 Solicitud de variedades desde: {DeviceId}", deviceId);

            // Reenviar solicitud a la aplicación de escritorio para que obtenga las variedades  
            await Clients.All.SendAsync("VariedadesRequested", deviceId);
        }

        // Método para que la aplicación de escritorio envíe las variedades al móvil  
        public async Task SendVariedadesListToMobile(string deviceId, object variedadesList)
        {
            if (_deviceConnections.TryGetValue(deviceId, out string? connectionId))
            {
                await Clients.Client(connectionId).SendAsync("VariedadesListReceived", variedadesList);
                _logger.LogInformation("📋 Lista de variedades enviada al móvil - Device: {DeviceId}", deviceId);
            }
            else
            {
                _logger.LogWarning("⚠️ Dispositivo no encontrado para envío de variedades: {DeviceId}", deviceId);
            }
        }
        // Método para notificar cambio en estado de uso del viaje
        // Método actualizado para rastrear qué cliente está usando qué viaje  
        public async Task NotifyTripInUse(string numeroGuia, bool enUso)
        {
            _logger.LogInformation("📱 Notificando estado de viaje: {NumeroGuia} - {EnUso}", numeroGuia, enUso);

            if (enUso)
            {
                // Registrar que este cliente está usando este viaje    
                _clientTrips[Context.ConnectionId] = numeroGuia;
                // NUEVO: Actualizar estado global  
                _globalTripsInUse[numeroGuia] = true;
                _logger.LogInformation("🔒 Cliente {ConnectionId} registrado usando viaje: {NumeroGuia}",
                                     Context.ConnectionId, numeroGuia);
            }
            else
            {
                // Remover el registro cuando se libera el viaje    
                _clientTrips.TryRemove(Context.ConnectionId, out _);
                // NUEVO: Actualizar estado global  
                _globalTripsInUse[numeroGuia] = false;
                _logger.LogInformation("🔓 Cliente {ConnectionId} liberó viaje: {NumeroGuia}",
                                     Context.ConnectionId, numeroGuia);
            }

            // Retransmitir a otros clientes (no al que envió)    
            await Clients.Others.SendAsync("TripInUseChanged", numeroGuia, enUso);
            _logger.LogInformation("📤 Notificación enviada a otros clientes");
        }
        public async Task RequestCurrentTripStatus()
        {
            _logger.LogInformation("🔄 Cliente {ConnectionId} solicitando estado actual de viajes", Context.ConnectionId);

            // Enviar estado actual de todos los viajes en uso  
            foreach (var trip in _globalTripsInUse)
            {
                if (trip.Value) // Solo viajes que están en uso  
                {
                    await Clients.Caller.SendAsync("TripInUseChanged", trip.Key, true);
                    _logger.LogInformation("📤 Estado inicial enviado: {NumeroGuia} - EN USO", trip.Key);
                }
            }

            _logger.LogInformation("✅ Sincronización inicial completada para cliente {ConnectionId}", Context.ConnectionId);
        }
        // NUEVO: Método para solicitar lista de embalajes bicolor  
        public async Task RequestBicolorPackagingTypes(string deviceId)
        {
            _deviceConnections[deviceId] = Context.ConnectionId;
            _logger.LogInformation("📱 Solicitud de tipos de embalaje bicolor desde: {DeviceId}", deviceId);

            // Reenviar solicitud a la aplicación de escritorio  
            await Clients.All.SendAsync("BicolorPackagingTypesRequested", deviceId);
        }

        // NUEVO: Método para enviar lista de embalajes bicolor al móvil  
        public async Task SendBicolorPackagingTypesToMobile(string deviceId, object packagingTypesList)
        {
            if (_deviceConnections.TryGetValue(deviceId, out string? connectionId))
            {
                await Clients.Client(connectionId).SendAsync("BicolorPackagingTypesReceived", packagingTypesList);
                _logger.LogInformation("📋 Lista de embalajes bicolor enviada al móvil - Device: {DeviceId}", deviceId);
            }
            else
            {
                _logger.LogWarning("⚠️ Dispositivo no encontrado para envío de embalajes bicolor: {DeviceId}", deviceId);
            }
        }
        // Agregar en Hubs/PalletHub.cs  VERSIO DE VERSION PARA MEJORA MODULOS OK.
        public async Task SendPalletInfoToMobile(string tripId, string infoMessage, string deviceId)
        {
            if (_deviceConnections.TryGetValue(deviceId, out string? connectionId))
            {
                await Clients.Client(connectionId).SendAsync("PalletInfo", tripId, infoMessage, deviceId);
                _logger.LogInformation("ℹ️ Mensaje informativo enviado al móvil - Trip: {TripId}, Device: {DeviceId}",
                                     tripId, deviceId);
            }
            else
            {
                _logger.LogWarning("⚠️ Dispositivo no encontrado para mensaje informativo: {DeviceId}", deviceId);
            }
        }
        // ============================================================================  
        // NUEVOS MÉTODOS PARA MÓDULO TESTEADOR  
        // ============================================================================  

        /// <summary>  
        /// Móvil (Testeador) solicita información completa de un pallet desde Packing_SJP  
        /// </summary>  
        public async Task RequestPalletInfo(string palletNumber, string deviceId)
        {
            _deviceConnections[deviceId] = Context.ConnectionId;
            _logger.LogInformation("🔍 [Testeador] Solicitud de info de pallet {PalletNumber} desde dispositivo {DeviceId}",
                                  palletNumber, deviceId);

            // Broadcast a todos los clientes de escritorio conectados  
            await Clients.Others.SendAsync("OnPalletInfoRequested", palletNumber, deviceId);
        }

        /// <summary>  
        /// Escritorio envía información del pallet al móvil solicitante (Testeador)  
        /// </summary>  
        public async Task SendPalletInfoToMobileTesteador(string palletDataJson, string deviceId, bool success, string errorMessage = "")
        {
            if (_deviceConnections.TryGetValue(deviceId, out string? connectionId))
            {
                await Clients.Client(connectionId).SendAsync("OnPalletInfoReceived", palletDataJson, deviceId, success, errorMessage);
                _logger.LogInformation("📤 [Testeador] Info de pallet enviada a dispositivo {DeviceId}, Success: {Success}",
                                      deviceId, success);
            }
            else
            {
                _logger.LogWarning("⚠️ [Testeador] Dispositivo no encontrado para envío de info: {DeviceId}", deviceId);
            }
        }

        /// <summary>  
        /// Móvil (Testeador) solicita eliminación de un pallet de Packing_SJP  
        /// </summary>  
        public async Task RequestPalletDeletion(string palletNumber, string deviceId)
        {
            _deviceConnections[deviceId] = Context.ConnectionId;
            _logger.LogInformation("🗑️ [Testeador] Solicitud de eliminación de pallet {PalletNumber} desde dispositivo {DeviceId}",
                                  palletNumber, deviceId);

            // Broadcast a escritorio para que procese la eliminación  
            await Clients.Others.SendAsync("OnPalletDeletionRequested", palletNumber, deviceId);
        }

        /// <summary>  
        /// Escritorio confirma resultado de eliminación al móvil (Testeador)  
        /// </summary>  
        public async Task SendDeletionResultToMobile(string palletNumber, string deviceId, bool success, string message)
        {
            if (_deviceConnections.TryGetValue(deviceId, out string? connectionId))
            {
                await Clients.Client(connectionId).SendAsync("OnPalletDeletionResult", palletNumber, deviceId, success, message);
                _logger.LogInformation("✅ [Testeador] Resultado de eliminación de {PalletNumber} enviado a {DeviceId}: {Success} - {Message}",
                                      palletNumber, deviceId, success, message);
            }
            else
            {
                _logger.LogWarning("⚠️ [Testeador] Dispositivo no encontrado para resultado de eliminación: {DeviceId}", deviceId);
            }
        }

    }
}