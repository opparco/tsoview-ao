using System;
using System.IO;
using TDCG;

public class BMPSaveDataTest
{
    public static void Main(string[] args)
    {
        if (args.Length != 1)
        {
            System.Console.WriteLine("Usage: BMPSaveDataTest <png file>");
            return;
        }

        string source_file = args[0];

        BMPSaveData data = new BMPSaveData();

        using (Stream stream = File.OpenRead(source_file))
            data.Read(stream);

        for (int i = 0; i < 32; i++)
        {
            Console.WriteLine(data.GetFileName(i));
        }
#if false
        for (int i = 0; i < 16; i++)
        {
            Console.WriteLine("0x{0:X8}", BitConverter.ToUInt32(data.GetBytes(i), 0));
        }
#endif

        Console.WriteLine(data.GetSliderValue(0)); // おっぱい
        Console.WriteLine(BitConverter.ToUInt32(data.GetBytes(1), 0));
        Console.WriteLine("0x{0:X8}", BitConverter.ToUInt32(data.GetBytes(2), 0)); // 固定
        Console.WriteLine("0x{0:X8}", BitConverter.ToUInt32(data.GetBytes(3), 0)); // key1
        Console.WriteLine(data.GetSliderValue(4)); // 姉妹
        Console.WriteLine(data.GetSliderValue(5));
        Console.WriteLine(data.GetSliderValue(6));
        Console.WriteLine(data.GetSliderValue(7)); // 胴まわり
        Console.WriteLine(data.GetSliderValue(8));
        Console.WriteLine("0x{0:X8}", BitConverter.ToUInt32(data.GetBytes(9), 0)); // 固定
        Console.WriteLine("0x{0:X8}", BitConverter.ToUInt32(data.GetBytes(10), 0)); // key2
        Console.WriteLine(data.GetSliderValue(11));
        Console.WriteLine("0x{0:X8}", BitConverter.ToUInt32(data.GetBytes(12), 0)); // 固定
        Console.WriteLine("0x{0:X8}", BitConverter.ToUInt32(data.GetBytes(13), 0)); // key3


        Console.WriteLine("");
        //data.SetFileName(0, "items/N0010BC1_A01");
        // おっぱい
        float oppai = 0.225f;
        data.SetSliderValue(0, oppai);
        Console.WriteLine("oppai {0:F4}", oppai);
        // 姉妹
        float age = 0.5f;
        data.SetSliderValue(4, age);
        Console.WriteLine("age {0:F4}", age);

        data.Save("out.thumbnail.png");

        //data.Save("tmp.png");
        
        //PNGFile png = new PNGFile();
        //png.Load("tmp.png");
        //png.Save("out.tdcgsav.png");
    }
}
