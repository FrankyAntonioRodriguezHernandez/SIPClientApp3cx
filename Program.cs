using System;
using System.Threading.Tasks;
using SIPSorcery.SIP;
using SIPSorcery.SIP.App;
using NAudio.Wave;

class Program
{
    private const string SIP_DOMAIN = "smabit.3cx.it";
    private const int SIP_PORT = 5060;
    private static string SIP_USERNAME = "29";
    private static string SIP_PASSWORD = "WEvB2aRAp8"; 
    private static WaveInEvent? _waveIn;
    private static WaveOutEvent? _waveOut;

    static async Task Main(string[] args)
    {
        Console.WriteLine("Cliente SIP para 3CX - .NET 8");
        Console.WriteLine($"Servidor: {SIP_DOMAIN}");
        Console.WriteLine($"Usuario: {SIP_USERNAME}");

        Console.Write("Ingrese el número a llamar: ");
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
            var userAgent = new SIPUserAgent(sipTransport, null);

            // 3. Registrar en 3CX con constructor correcto para v8.0.11
            var regUserAgent = new SIPRegistrationUserAgent(
                sipTransport,
                SIP_USERNAME,
                SIP_PASSWORD,
                SIP_DOMAIN,
                SIP_PORT);

            regUserAgent.RegistrationFailed += (uri, resp, err) => 
                Console.WriteLine($"Error de registro: {err} (Código: {resp?.StatusCode})");
            
            regUserAgent.RegistrationSuccessful += (uri, resp) => 
                Console.WriteLine("Registro SIP exitoso!");

            regUserAgent.Start();
            Console.WriteLine("Registrando en 3CX...");
            await Task.Delay(3000);

            // 4. Configurar llamada con URI correctamente formateado
            var callDescriptor = new SIPCallDescriptor(
                SIP_USERNAME,
                SIP_PASSWORD,
                SIP_DOMAIN,
                $"sip:{destinationNumber}@{SIP_DOMAIN}", // Formato corregido
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

    private static void SetupAudioDevices()
    {
        try
        {
            _waveIn = new WaveInEvent
            {
                WaveFormat = new WaveFormat(8000, 16, 1),
                BufferMilliseconds = 50
            };
            
            _waveOut = new WaveOutEvent();
            var waveProvider = new BufferedWaveProvider(_waveIn.WaveFormat);
            _waveOut.Init(waveProvider);
            _waveOut.Play();

            Console.WriteLine("Audio configurado para 3CX:");
            Console.WriteLine("- Formato: PCM 8000Hz, 16 bits, mono");
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