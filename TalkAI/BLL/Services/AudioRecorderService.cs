using BLL.Interface;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;
namespace BLL.Services
{
    public class AudioRecorderService : IAudioRecorderService
    {
        public async Task<byte[]> RecordFromMicAsync(int durationSeconds)
        {
            using var ms = new MemoryStream();
            using var waveIn = new WaveInEvent();
            waveIn.WaveFormat = new WaveFormat(16000, 16, 1);

            using (var waveWriter = new WaveFileWriter(ms, waveIn.WaveFormat))
            {
                waveIn.DataAvailable += (s, e) => waveWriter.Write(e.Buffer, 0, e.BytesRecorded);
                waveIn.StartRecording();
                await Task.Delay(durationSeconds * 500);
                waveIn.StopRecording();
                waveWriter.Flush();
            }

            return ms.ToArray();
        }

    }
}
