using System;
using System.Threading.Tasks;
using SIPSorcery.SIP;
using SIPSorcery.SIP.App;
using NAudio.Wave;

class Program
{
    private static string SIP_SERVER = "smabit.3cx.it";
    private static string SIP_USERNAME = "32";
    private static string SIP_PASSWORD = "Franky2025";
    private static WaveInEvent? _waveIn;
    private static WaveOutEvent? _waveOut;

    static async Task Main(string[] args)
    {
        Console.WriteLine("Cliente SIP para 3CX - .NET 8");
        Console.WriteLine($"Servidor: {SIP_SERVER}");
        Console.WriteLine($"Usuario: {SIP_USERNAME}");

        Console.Write("Ingrese el número a llamar (ej: 33): ");
        string destinationNumber = Console.ReadLine();

        try
        {
            // Configurar audio
            SetupAudioDevices();

            // Configurar el transporte SIP
            var sipTransport = new SIPTransport();
            var userAgent = new SIPUserAgent(sipTransport, null);

            // Registrar en el servidor 3CX con el constructor correcto (5 parámetros)
            var regUserAgent = new SIPRegistrationUserAgent(
                sipTransport,
                SIP_USERNAME,    // username
                SIP_PASSWORD,    // password
                SIP_SERVER,      // server
                5060);           // port

            regUserAgent.RegistrationFailed += (uri, resp, err) => 
                Console.WriteLine($"Error de registro: {err} (Código: {resp?.StatusCode})");
            
            regUserAgent.RegistrationSuccessful += (uri, resp) => 
                Console.WriteLine("Registro SIP exitoso!");

            regUserAgent.Start();

            Console.WriteLine("Registrando en el servidor 3CX...");
            await Task.Delay(3000);

            // Configurar la llamada con URI correctamente formateado
            var callDescriptor = new SIPCallDescriptor(
                SIP_USERNAME,
                SIP_PASSWORD,
                SIP_SERVER,
                $"sip:{destinationNumber}@{SIP_SERVER}",
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

            // Realizar la llamada
            bool callResult = await userAgent.Call(callDescriptor, null);

            if (callResult)
            {
                Console.WriteLine("Llamada conectada. Presione 'h' para colgar...");
                
                while (Console.ReadKey(true).Key != ConsoleKey.H)
                {
                    // Mantener la llamada activa
                }

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
        }

        Console.WriteLine("Presione cualquier tecla para salir...");
        Console.ReadKey();
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

            Console.WriteLine("Audio configurado para VoIP:");
            Console.WriteLine($"- Formato: 8000Hz, 16 bits, mono");
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