using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace OmronFinsSample
{
    public sealed class FinsUdpClient : IDisposable
    {
        private readonly string plcIp;
        private readonly int plcPort;
        private readonly byte destinationNode; // DA1
        private readonly byte sourceNode;      // SA1
        private UdpClient? udpClient;
        private byte serviceId = 0x00;

        public FinsUdpClient(string plcIp, int plcPort = 9600)
        {
            this.plcIp = plcIp;
            this.plcPort = plcPort;

            var plcAddrBytes = IPAddress.Parse(plcIp).GetAddressBytes();
            destinationNode = plcAddrBytes[3];
            sourceNode = GetLocalNodeForRemote(plcIp);
        }

        public void Connect()
        {
            udpClient = new UdpClient();
            udpClient.Connect(plcIp, plcPort);
        }

        public async Task<byte[]> ReadDmAsync(ushort dmAddress, ushort wordCount = 1, int timeoutMs = 2000)
        {
            if (udpClient == null)
                throw new InvalidOperationException("Önce Connect() çağırın.");

            udpClient.Client.ReceiveTimeout = timeoutMs;

            var sid = unchecked(++serviceId);
            var frame = BuildMemoryAreaReadFrame(
                sourceNode, destinationNode, sid,
                memoryArea: 0x82, // DM Area
                address: dmAddress,
                count: wordCount
            );

            await udpClient.SendAsync(frame, frame.Length);

            // Receive with timeout (CancellationToken)
            using var cts = new CancellationTokenSource(timeoutMs);
            var response = await udpClient.ReceiveAsync(cts.Token);
            return ParseMemoryAreaReadResponse(response.Buffer, expectedSid: sid, expectedWords: wordCount);
        }

        public void Dispose()
        {
            udpClient?.Dispose();
            udpClient = null;
        }

        private static byte[] BuildMemoryAreaReadFrame(byte sa1, byte da1, byte sid, byte memoryArea, ushort address, ushort count)
        {
            // FINS/UDP Header (10 bytes): ICF, RSV, GCT, DNA, DA1, DA2, SNA, SA1, SA2, SID
            // Command: 0x01 0x01 (Memory Area Read)
            // Parameters: memoryArea, addrHi, addrLo, bitAddr, countHi, countLo
            return new byte[]
            {
                0x80,             // ICF
                0x00,             // RSV/RSC
                0x02,             // GCT
                0x00,             // DNA
                da1,              // DA1 (PLC Node)
                0x00,             // DA2 (Unit - CPU)
                0x00,             // SNA
                sa1,              // SA1 (PC Node)
                0x00,             // SA2
                sid,              // SID
                0x01, 0x01,       // Command: Memory Area Read
                memoryArea,                      // Memory area (DM:0x82)
                (byte)(address >> 8),            // Address high
                (byte)(address & 0xFF),          // Address low
                0x00,                            // Bit address
                (byte)(count >> 8),              // Count high
                (byte)(count & 0xFF)             // Count low
            };
        }

        private static byte[] ParseMemoryAreaReadResponse(byte[] buffer, byte expectedSid, ushort expectedWords)
        {
            // Minimum: 10 (header) + 2 (cmd) + 2 (end code) + data
            if (buffer.Length < 14)
                throw new Exception("Yanıt çok kısa.");

            // Kontrol: SID eşleşmesi (header byte[9])
            var sid = buffer[9];
            if (sid != expectedSid)
                throw new Exception($"SID eşleşmedi. Beklenen: {expectedSid}, Gelen: {sid}");

            // End code: buffer[12..14]
            var endCode = (ushort)((buffer[12] << 8) | buffer[13]);
            if (endCode != 0x0000)
                throw new Exception($"PLC hata kodu: 0x{endCode:X4}");

            // Veri 14. bayttan başlar
            int dataOffset = 14;
            int expectedBytes = expectedWords * 2;
            if (buffer.Length < dataOffset + expectedBytes)
                throw new Exception("Beklenenden az veri geldi.");

            var data = new byte[expectedBytes];
            Array.Copy(buffer, dataOffset, data, 0, expectedBytes);
            return data;
        }

        private static byte GetLocalNodeForRemote(string remoteIp)
        {
            using var s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            // Bağlanma, yerel IP'nin aynı ağdaki uygun arabirimden seçilmesini sağlar
            s.Connect(IPAddress.Parse(remoteIp), 9);
            var local = (IPEndPoint)s.LocalEndPoint!;
            return local.Address.GetAddressBytes()[3];
        }
    }

    internal class Program
    {
        static async Task Main()
        {
            var plcIp = "192.168.250.1";
            var plcPort = 9600;

            using var client = new FinsUdpClient(plcIp, plcPort);

            try
            {
                Console.WriteLine("PLC'ye bağlanılıyor...");
                client.Connect();
                Console.WriteLine("Bağlantı sağlandı.");

                // D0 (DM 0) adresinden 1 kelime (2 byte) oku
                var raw = await client.ReadDmAsync(dmAddress: 0, wordCount: 1);
                Console.WriteLine("Alınan Veri (Raw): " + BitConverter.ToString(raw).Replace("-", " "));

                // Big-endian 2 byte -> UInt16
                if (raw.Length == 2)
                {
                    ushort value = (ushort)((raw[0] << 8) | raw[1]);
                    Console.WriteLine($"D0 adresindeki sayısal değer: {value}");
                }
                else
                {
                    Console.WriteLine("Beklenmeyen veri uzunluğu.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hata oluştu: {ex.Message}");
            }
            finally
            {
                Console.WriteLine("Bağlantı kapatıldı.");
            }
        }
    }
}
