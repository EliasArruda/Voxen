using System.Buffers.Binary;
namespace Voxen.Services;

/// <summary>Three stereo biquads at the decoder's fixed 48 kHz rate, with boost headroom.</summary>
public sealed class AudioToneProcessor
{
    private readonly Filter[,] _filters = new Filter[2, 3];
    private readonly double _headroom;
    private readonly bool _flat;
    public AudioToneProcessor(AudioTone tone)
    {
        tone = tone.Safe(); _flat = tone == new AudioTone();
        _headroom = Math.Pow(10, -(Math.Max(0,tone.Bass) + Math.Max(0,tone.Mid) + Math.Max(0,tone.Treble)) / 20);
        for (var channel = 0; channel < 2; channel++)
        {
            _filters[channel, 0] = new Filter(100, tone.Bass, 0);
            _filters[channel, 1] = new Filter(1000, tone.Mid, 1);
            _filters[channel, 2] = new Filter(8000, tone.Treble, 2);
        }
    }
    public void Process(Span<byte> pcm)
    {
        if (_flat) return;
        for (var offset = 0; offset + 3 < pcm.Length; offset += 4)
            for (var channel = 0; channel < 2; channel++)
            {
                var slice = pcm.Slice(offset + channel * 2, 2);
                var sample = BinaryPrimitives.ReadInt16LittleEndian(slice) / 32768d * _headroom;
                for (var band = 0; band < 3; band++) sample = _filters[channel, band].Apply(sample);
                BinaryPrimitives.WriteInt16LittleEndian(slice, (short)Math.Clamp(Math.Round(sample * 32768), short.MinValue, short.MaxValue));
            }
    }
    private sealed class Filter
    {
        private readonly double b0,b1,b2,a1,a2;
        private double x1,x2,y1,y2;
        public Filter(double frequency, double gain, int kind)
        {
            var a=Math.Pow(10,gain/40); var w=2*Math.PI*frequency/48000; var c=Math.Cos(w); var s=Math.Sin(w);
            double n0,n1,n2,d0,d1,d2;
            if (kind==1) { var alpha=s/(2*.707); n0=1+alpha*a;n1=-2*c;n2=1-alpha*a;d0=1+alpha/a;d1=-2*c;d2=1-alpha/a; }
            else
            {
                var k=s*Math.Sqrt(2*a);
                if(kind==0) { n0=a*((a+1)-(a-1)*c+k);n1=2*a*((a-1)-(a+1)*c);n2=a*((a+1)-(a-1)*c-k);d0=(a+1)+(a-1)*c+k;d1=-2*((a-1)+(a+1)*c);d2=(a+1)+(a-1)*c-k; }
                else { n0=a*((a+1)+(a-1)*c+k);n1=-2*a*((a-1)+(a+1)*c);n2=a*((a+1)+(a-1)*c-k);d0=(a+1)-(a-1)*c+k;d1=2*((a-1)-(a+1)*c);d2=(a+1)-(a-1)*c-k; }
            }
            b0=n0/d0;b1=n1/d0;b2=n2/d0;a1=d1/d0;a2=d2/d0;
        }
        public double Apply(double value) { var result=b0*value+b1*x1+b2*x2-a1*y1-a2*y2;x2=x1;x1=value;y2=y1;y1=result;return result; }
    }
}
