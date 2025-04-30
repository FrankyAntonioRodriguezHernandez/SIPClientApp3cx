using System;
using System.Net;
using System.Threading.Tasks;
using SIPSorcery.SIP;
using SIPSorcery.SIP.App;
using NAudio.Wave;

class Program
{
    // Configuración para AntiSIP
    private const string SIP_DOMAIN = "sip.antisip.com";
    private const int SIP_PORT = 5060;
    private static string SIP_USERNAME = "frankyan";
    private static string SIP_PASSWORD = "hb8zRUcD8CTGS6x";
    private static WaveInEvent? _waveIn;
    private static WaveOutEvent? _waveOut;

    static async Task Main(string[] args)
    {
        Console.WriteLine("Cliente SIP para AntiSIP - .NET 8");
        Console.WriteLine($"Usuario: {SIP_USERNAME}@{SIP_DOMAIN}");

        Console.Write("Ingrese el número a llamar (prueba con 100 para eco): ");
        string? destinationNumber = Console.ReadLine();

        if (string.IsNullOrEmpty(destinationNumber))
        {
            Console.WriteLine("Número inválido");
            return;
        }

        try
        {
            // 1. Configurar audio
            SetupAudioDevices();

            // 2. Configurar transporte SIP
            var sipTransport = new SIPTransport();
            var udpChannel = new SIPUDPChannel(new IPEndPoint(IPAddress.Any, 0));
            sipTransport.AddSIPChannel(udpChannel);
            
            // Añade esto después de crear el SIPTransport
            sipTransport.EnableTraceLogs();

            var userAgent = new SIPUserAgent(sipTransport, null);

            // 3. Registrar en AntiSIP
            var registrarUri = SIPURI.ParseSIPURI($"sip:{SIP_DOMAIN}:{SIP_PORT}");
            
            var regUserAgent = new SIPRegistrationUserAgent(
                sipTransport,
                SIP_USERNAME,
                SIP_PASSWORD,
                registrarUri.ToString(),
                SIP_REGISTRATION_DEFAULT_EXPIRY);

            regUserAgent.RegistrationFailed += (uri, resp, err) => 
                Console.WriteLine($"Error de registro: {err} (Código: {resp?.StatusCode})");
            
            regUserAgent.RegistrationSuccessful += (uri, resp) => 
                Console.WriteLine("Registro SIP exitoso en AntiSIP!");

            Console.WriteLine("Registrando en AntiSIP...");
            regUserAgent.Start();

            // Esperar 3 segundos para que el registro se complete
            await Task.Delay(3000);

            // 4. Configurar llamada (URI corregido)
            var destinationUri = SIPURI.ParseSIPURI($"sip:{destinationNumber}@{SIP_DOMAIN}:{SIP_PORT}");
            
            var callDescriptor = new SIPCallDescriptor(
                SIP_USERNAME,
                SIP_PASSWORD,
                SIP_DOMAIN,
                destinationUri.ToString(), // Asegurar que el URI tenga el formato correcto
                null, null, null, null,
                SIPCallDirection.Out,
                "application/sdp",
                null,
                null);

            Console.WriteLine($"Llamando a {destinationNumber}...");
            
            userAgent.ClientCallFailed += (dialog, error, message) => 
                Console.WriteLine($"Error en llamada: {message}");
            
            userAgent.OnCallHungup += (dialog) => 
                Console.WriteLine("Llamada finalizada");

            // 5. Realizar llamada
            bool callResult = await userAgent.Call(callDescriptor, null);

            if (callResult)
            {
                Console.WriteLine("Llamada conectada. Presione 'h' para colgar...");
                while (Console.ReadKey(true).Key != ConsoleKey.H) { }
                userAgent.Hangup();
            }
            else
            {
                Console.WriteLine("No se pudo conectar la llamada");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        finally
        {
            CleanupAudio();
            Console.WriteLine("Presione cualquier tecla para salir...");
            Console.ReadKey();
        }
    }

    private const int SIP_REGISTRATION_DEFAULT_EXPIRY = 120;

    private static void SetupAudioDevices()
    {
        try
        {
            _waveIn = new WaveInEvent
            {
                WaveFormat = new WaveFormat(8000, 16, 1), // G.711 compatible
                BufferMilliseconds = 50
            };
            
            _waveOut = new WaveOutEvent();
            var waveProvider = new BufferedWaveProvider(_waveIn.WaveFormat);
            _waveOut.Init(waveProvider);
            _waveOut.Play();

            Console.WriteLine("Audio configurado para VoIP:");
            Console.WriteLine("- Formato: PCM 8000Hz, 16 bits, mono (G.711)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error configurando audio: {ex.Message}");
        }
    }

    private static void CleanupAudio()
    {
        _waveIn?.StopRecording();
        _waveIn?.Dispose();
        _waveOut?.Stop();
        _waveOut?.Dispose();
    }
}