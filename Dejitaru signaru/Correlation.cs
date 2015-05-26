using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dejitaru_signaru
{
    public class Correlation
    {

        /// <summary>
        /// Determines the correlation between wav file1 and file2 and stores result int arr
        /// </summary>
        /// <param name="file1"></param>
        /// <param name="file2"></param>
        /// <param name="arr"></param>
        /// <param name="courseness">A value of one is lossless</param>
        public static void Correlate(string file1, string file2, out double[] arr, int courseness=100)
        {
            double[] data1;
            double[] data2;
            WaveFormat info1;
            WaveFormat info2;            
            ReadWaveFile(file1, out data1, out info1, courseness);
            ReadWaveFile(file2, out data2, out info2, courseness);
            alglib.corrr1dcircular(data1, data1.Length, data2, data2.Length, out arr);

        }

        /// <summary>
        /// Reads wave file file_name and stores result in arr and information about file in waveInfo
        /// </summary>
        /// <param name="file_name"></param>
        /// <param name="arr"></param>
        /// <param name="waveInfo"></param>
        /// <param name="courseness">Determines what samples to keep. A value of one is lossless</param>
        public static void ReadWaveFile(string file_name, out double[] arr, out WaveFormat waveInfo, int courseness)
        {
            NAudio.Wave.WaveFileReader reader = new NAudio.Wave.WaveFileReader(file_name);

            long samplesDesired = reader.SampleCount;
            byte[] buffer = new byte[samplesDesired * 4];
            short[] left = new short[samplesDesired];
            short[] right = new short[samplesDesired];
            int bytesRead = reader.Read(buffer, 0, (int)reader.Length);
            int index = 0;
            for (int sample = 0; sample < bytesRead / 4; sample++)
            {
                left[sample] = BitConverter.ToInt16(buffer, index);
                index += 2;
                right[sample] = BitConverter.ToInt16(buffer, index);
                index += 2;
            }
            float max = 0;
            for (int i = 0; i < left.Length; i++)
            {
                if (left[i] > max)
                    max = left[i];
            }
            arr = new double[(int)Math.Ceiling((float)left.Length / courseness)];
            for (int i = 0; i < left.Length; i += courseness)
            {
                arr[i / courseness] = left[i] / max;
            }
            reader.Close();
            waveInfo = reader.WaveFormat;
        }

    }
}
