using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace takip.Services
{
    /// <summary>
    /// Omron FINS/UDP protokolü ile PLC bağlantısı
    /// </summary>
    public sealed class FinsUdpClient : IDisposable
    {
        private readonly string plcIp;
        private readonly int plcPort;
        private readonly byte destinationNode; // DA1
        private readonly byte sourceNode;      // SA1
        private UdpClient? udpClient;
        private byte serviceId = 0x00;
        private readonly object _sidLock = new object();

        public FinsUdpClient(string plcIp, int plcPort = 9600)
        {
            this.plcIp = plcIp;
            this.plcPort = plcPort;

            var plcAddrBytes = IPAddress.Parse(plcIp).GetAddressBytes();
            destinationNode = plcAddrBytes[3];
            sourceNode = GetLocalNodeForRemote(plcIp);
        }

        /// <summary>
        /// Memory Area Bit Read (0102) - HR alanından bit okuma.
        /// Hxx.yy gibi adresler için uygundur. PLC bir bit için 1 byte (0x00 veya 0x01) döndürür.
        /// </summary>
        public async Task<byte[]> ReadHBitAsync(ushort hAddress, byte bitAddress, ushort bitCount = 1, int timeoutMs = 2000)
        {
            if (udpClient == null)
                throw new InvalidOperationException("Önce Connect() çağırın.");

            udpClient.Client.ReceiveTimeout = timeoutMs;

            byte sid;
            lock (_sidLock)
            {
                sid = unchecked(++serviceId);
            }

            // 0102: Memory Area Bit Read - 18 byte frame (10 byte header + 2 byte command + 6 byte params)
            var frame = new byte[18];
            frame[0] = 0x80; // ICF
            frame[1] = 0x00; // RSV
            frame[2] = 0x02; // GCT
            frame[3] = 0x00; // DNA
            frame[4] = destinationNode; // DA1
            frame[5] = 0x00; // DA2 (CPU)
            frame[6] = 0x00; // SNA
            frame[7] = sourceNode; // SA1
            frame[8] = 0x00; // SA2
            frame[9] = sid;  // SID
            frame[10] = 0x01; // MRC
            frame[11] = 0x02; // SRC (0102: Memory Area Bit Read)
            frame[12] = 0xB2; // HR area
            frame[13] = (byte)(hAddress >> 8);
            frame[14] = (byte)(hAddress & 0xFF);
            frame[15] = bitAddress; // bit address (0..15)
            frame[16] = (byte)(bitCount >> 8);
            frame[17] = (byte)(bitCount & 0xFF);

            await udpClient.SendAsync(frame, frame.Length);

            using var cts = new CancellationTokenSource(timeoutMs);
            var response = await udpClient.ReceiveAsync(cts.Token);

            // Parse response: header[9]=SID, [12..13]=end code, data starts at 14, length=bitCount bytes (0x00/0x01)
            var buffer = response.Buffer;
            if (buffer.Length < 14)
                throw new Exception("Yanıt çok kısa.");
            var rsid = buffer[9];
            if (rsid != sid)
                throw new Exception($"SID eşleşmedi. Beklenen: {sid}, Gelen: {rsid}");
            var endCode = (ushort)((buffer[12] << 8) | buffer[13]);
            if (endCode != 0x0000)
                throw new Exception($"PLC hata kodu: 0x{endCode:X4}");
            int dataOffset = 14;
            int expectedBytes = bitCount;
            if (buffer.Length < dataOffset + expectedBytes)
                throw new Exception("Beklenenden az veri geldi (bit).");
            var data = new byte[expectedBytes];
            Array.Copy(buffer, dataOffset, data, 0, expectedBytes);
            return data;
        }

        public async Task<byte[]> ReadWrAsync(ushort wAddress, ushort wordCount = 1, int timeoutMs = 2000)
        {
            if (udpClient == null)
                throw new InvalidOperationException("Önce Connect() çağırın.");

            udpClient.Client.ReceiveTimeout = timeoutMs;

            byte sid;
            lock (_sidLock)
            {
                sid = unchecked(++serviceId);
            }
            var frame = BuildMemoryAreaReadFrame(
                sourceNode, destinationNode, sid,
                memoryArea: 0xB1, // WR Word area (Work Relay)
                address: wAddress,
                count: wordCount
            );

            await udpClient.SendAsync(frame, frame.Length);

            using var cts = new CancellationTokenSource(timeoutMs);
            var response = await udpClient.ReceiveAsync(cts.Token);
            return ParseMemoryAreaReadResponse(response.Buffer, expectedSid: sid, expectedWords: wordCount);
        }

        

        public void Connect()
        {
            try
            {
                udpClient?.Dispose();
                udpClient = new UdpClient();
                udpClient.Connect(plcIp, plcPort);
                System.Diagnostics.Debug.WriteLine($"[FinsUdpClient] PLC'ye bağlanıldı: {plcIp}:{plcPort}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FinsUdpClient] Bağlantı hatası: {ex.Message}");
                udpClient?.Dispose();
                udpClient = null;
                throw;
            }
        }

        public async Task<byte[]> ReadDmAsync(ushort dmAddress, ushort wordCount = 1, int timeoutMs = 2000)
        {
            if (udpClient == null)
                throw new InvalidOperationException("Önce Connect() çağırın.");

            udpClient.Client.ReceiveTimeout = timeoutMs;

            byte sid;
            lock (_sidLock)
            {
                sid = unchecked(++serviceId);
            }
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

        public async Task<byte[]> ReadHAsync(ushort hAddress, ushort wordCount = 1, int timeoutMs = 2000)
        {
            if (udpClient == null)
                throw new InvalidOperationException("Önce Connect() çağırın.");

            udpClient.Client.ReceiveTimeout = timeoutMs;

            byte sid;
            lock (_sidLock)
            {
                sid = unchecked(++serviceId);
            }
            var frame = BuildMemoryAreaReadFrame(
                sourceNode, destinationNode, sid,
                memoryArea: 0xB2, // HR Word area (Omron 'H' addresses as words)
                address: hAddress,
                count: wordCount
            );

            await udpClient.SendAsync(frame, frame.Length);

            // Receive with timeout (CancellationToken)
            using var cts = new CancellationTokenSource(timeoutMs);
            var response = await udpClient.ReceiveAsync(cts.Token);
            return ParseMemoryAreaReadResponse(response.Buffer, expectedSid: sid, expectedWords: wordCount);
        }

        private static byte[] BuildMemoryAreaReadFrame(byte sa1, byte da1, byte sid, byte memoryArea, ushort address, ushort count)
        {
            // 0101: Memory Area Read - 18 byte frame (10 byte header + 2 byte command + 6 byte params)
            var frame = new byte[18];
            frame[0] = 0x80; // ICF
            frame[1] = 0x00; // RSV
            frame[2] = 0x02; // GCT
            frame[3] = 0x00; // DNA
            frame[4] = da1;  // DA1
            frame[5] = 0x00; // DA2
            frame[6] = 0x00; // SNA
            frame[7] = sa1;  // SA1
            frame[8] = 0x00; // SA2
            frame[9] = sid;  // SID
            frame[10] = 0x01; // MRC
            frame[11] = 0x01; // SRC
            frame[12] = memoryArea; // Memory Area
            frame[13] = (byte)(address >> 8); // Address (high)
            frame[14] = (byte)(address & 0xFF); // Address (low)
            frame[15] = 0x00; // Bit address
            frame[16] = (byte)(count >> 8); // Count (high)
            frame[17] = (byte)(count & 0xFF); // Count (low)
            return frame;
        }

        private static byte[] ParseMemoryAreaReadResponse(byte[] buffer, byte expectedSid, ushort expectedWords)
        {
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

        /// <summary>
        /// DM Memory Area Read (0101) - Batch okuma
        /// </summary>
        public async Task<byte[]> ReadDmBatchAsync(ushort startAddress, ushort wordCount, int timeoutMs = 3000)
        {
            if (udpClient == null)
                throw new InvalidOperationException("Önce Connect() çağırın.");

            udpClient.Client.ReceiveTimeout = timeoutMs;

            byte sid;
            lock (_sidLock)
            {
                sid = unchecked(++serviceId);
            }

            // 0101: Memory Area Read - Legacy client ile aynı frame yapısı (18 byte)
            var frame = new byte[18];
            frame[0] = 0x80; // ICF
            frame[1] = 0x00; // RSV
            frame[2] = 0x02; // GCT
            frame[3] = 0x00; // DNA
            frame[4] = destinationNode; // DA1
            frame[5] = 0x00; // DA2
            frame[6] = 0x00; // SNA
            frame[7] = sourceNode; // SA1
            frame[8] = 0x00; // SA2
            frame[9] = sid; // SID
            frame[10] = 0x01; // MRC
            frame[11] = 0x01; // SRC
            frame[12] = 0x82; // Memory Area: DM (0x82)
            frame[13] = (byte)(startAddress >> 8); // Address High
            frame[14] = (byte)(startAddress & 0xFF); // Address Low
            frame[15] = 0x00; // Bit address (word read)
            frame[16] = (byte)(wordCount >> 8); // Count High
            frame[17] = (byte)(wordCount & 0xFF); // Count Low

            await udpClient.SendAsync(frame, frame.Length);

            var response = await udpClient.ReceiveAsync();
            var responseData = response.Buffer;

            if (responseData.Length < 14)
                throw new InvalidOperationException("Geçersiz yanıt uzunluğu");

            // SID kontrolü (byte[9])
            if (responseData[9] != sid)
                throw new InvalidOperationException($"SID eşleşmedi. Beklenen: {sid}, Gelen: {responseData[9]}");

            // End code kontrolü (byte[12-13])
            var endCode = (ushort)((responseData[12] << 8) | responseData[13]);
            if (endCode != 0x0000)
                throw new InvalidOperationException($"PLC hata kodu: 0x{endCode:X4}");

            // Veri 14. bayttan başlar
            var expectedBytes = wordCount * 2;
            var data = new byte[expectedBytes];
            Array.Copy(responseData, 14, data, 0, Math.Min(expectedBytes, responseData.Length - 14));

            return data;
        }

        /// <summary>
        /// HR Memory Area Read (0101) - Batch okuma
        /// </summary>
        public async Task<byte[]> ReadHBatchAsync(ushort startAddress, ushort wordCount, int timeoutMs = 3000)
        {
            if (udpClient == null)
                throw new InvalidOperationException("Önce Connect() çağırın.");

            udpClient.Client.ReceiveTimeout = timeoutMs;

            byte sid;
            lock (_sidLock)
            {
                sid = unchecked(++serviceId);
            }

            // 0101: Memory Area Read - Legacy client ile aynı frame yapısı (18 byte)
            var frame = new byte[18];
            frame[0] = 0x80; // ICF
            frame[1] = 0x00; // RSV
            frame[2] = 0x02; // GCT
            frame[3] = 0x00; // DNA
            frame[4] = destinationNode; // DA1
            frame[5] = 0x00; // DA2
            frame[6] = 0x00; // SNA
            frame[7] = sourceNode; // SA1
            frame[8] = 0x00; // SA2
            frame[9] = sid; // SID
            frame[10] = 0x01; // MRC
            frame[11] = 0x01; // SRC
            frame[12] = 0xB2; // Memory Area: HR (0xB2) - Legacy client ile aynı
            frame[13] = (byte)(startAddress >> 8); // Address High
            frame[14] = (byte)(startAddress & 0xFF); // Address Low
            frame[15] = 0x00; // Bit address (word read)
            frame[16] = (byte)(wordCount >> 8); // Count High
            frame[17] = (byte)(wordCount & 0xFF); // Count Low

            await udpClient.SendAsync(frame, frame.Length);

            var response = await udpClient.ReceiveAsync();
            var responseData = response.Buffer;

            if (responseData.Length < 14)
                throw new InvalidOperationException("Geçersiz yanıt uzunluğu");

            // SID kontrolü (byte[9])
            if (responseData[9] != sid)
                throw new InvalidOperationException($"SID eşleşmedi. Beklenen: {sid}, Gelen: {responseData[9]}");

            // End code kontrolü (byte[12-13])
            var endCode = (ushort)((responseData[12] << 8) | responseData[13]);
            if (endCode != 0x0000)
                throw new InvalidOperationException($"PLC hata kodu: 0x{endCode:X4}");

            // Veri 14. bayttan başlar
            var expectedBytes = wordCount * 2;
            var data = new byte[expectedBytes];
            Array.Copy(responseData, 14, data, 0, Math.Min(expectedBytes, responseData.Length - 14));

            return data;
        }

        

        public void Dispose()
        {
            udpClient?.Dispose();
            udpClient = null;
        }
    }
}
