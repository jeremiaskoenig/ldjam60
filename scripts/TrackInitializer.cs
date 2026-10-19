using Godot;
using System;

[Tool]
public partial class TrackInitializer : Path3D
{
    [ExportToolButton("Initialize")]
    public Callable Initialize => Callable.From(Init);

    private void Init()
    {
        if (Curve.PointCount == 0)
        {
            Vector3[] points = 
            [
                new Vector3(1167.0746f, 7.6483555f, 491.97772f),
                new Vector3(1171.7183f, 7.9941125f, 551.3545f),
                new Vector3(1191.1653f, 7.143031f, 608.4159f),
                new Vector3(1192.89f, 7.0717316f, 611.43005f),
                new Vector3(1229.3375f, 5.7695317f, 662.7221f),
                new Vector3(1311.1201f, 4.21514f, 758.19867f),
                new Vector3(1368.5935f, 5.100838f, 835.39124f),
                new Vector3(1393.5784f, 6.2269206f, 887.68634f),
                new Vector3(1401.6185f, 6.8968573f, 937.04236f),
                new Vector3(1396.3275f, 6.8272257f, 1005.8123f),
                new Vector3(1380.2499f, 5.8557315f, 1075.9246f),
                new Vector3(1348.9412f, 4.259991f, 1157.9215f),
                new Vector3(1306.3577f, 3.1712031f, 1234.1948f),
                new Vector3(1264.4514f, 3.0640793f, 1284.388f),
                new Vector3(1214.4733f, 3.687986f, 1317.7509f),
                new Vector3(1163.1732f, 4.8458185f, 1329.9961f),
                new Vector3(1095.3093f, 6.8457284f, 1328.6665f),
                new Vector3(1037.5005f, 8.602303f, 1319.5869f),
                new Vector3(980.5987f, 9.989438f, 1307.91f),
                new Vector3(913.2015f, 10.871627f, 1290.7999f),
                new Vector3(819.381f, 10.682465f, 1268.8317f),
                new Vector3(666.69775f, 8.580198f, 1239.6613f),
                new Vector3(603.21765f, 7.8906307f, 1219.8257f),
                new Vector3(530.6739f, 7.926736f, 1182.9697f),
                new Vector3(467.06738f, 8.93691f, 1127.7925f),
                new Vector3(411.8352f, 10.510171f, 1059.4728f),
                new Vector3(367.39722f, 12.072397f, 990.7428f),
                new Vector3(336.7301f, 13.104849f, 923.539f),
                new Vector3(330.24463f, 13.381221f, 890.03204f),
                new Vector3(341.6776f, 13.413027f, 829.66943f),
                new Vector3(360.62714f, 12.867299f, 771.0694f),
                new Vector3(368.37585f, 12.018003f, 722.0952f),
                new Vector3(366.4781f, 10.604906f, 662.6511f),
                new Vector3(360.7193f, 8.104782f, 571.03064f),
                new Vector3(364.04584f, 7.0433125f, 530.0765f),
                new Vector3(379.27826f, 5.8562217f, 478.8634f),
                new Vector3(411.81085f, 5.129467f, 431.15613f),
                new Vector3(454.1552f, 5.0301847f, 389.4434f),
                new Vector3(526.99036f, 5.7612033f, 335.99362f),
                new Vector3(619.49536f, 6.9884825f, 280.7802f),
                new Vector3(712.04834f, 5.9737844f, 237.27177f),
                new Vector3(751.4569f, 5.18528f, 222.1123f),
                new Vector3(769.144f, 4.8890986f, 213.32025f),
                new Vector3(794.5783f, 4.5430036f, 187.68283f),
                new Vector3(818.84894f, 4.3263683f, 166.11346f),
                new Vector3(835.73364f, 4.231848f, 161.08963f),
                new Vector3(850.9125f, 4.179523f, 165.87659f),
                new Vector3(914.6643f, 4.1668153f, 189.53265f),
                new Vector3(1056.9198f, 4.1699696f, 244.17198f),
                new Vector3(1157.2867f, 4.1817055f, 280.91437f),
                new Vector3(1186.161f, 4.2119365f, 292.45398f),
                new Vector3(1196.8599f, 4.30514f, 300.46625f),
                new Vector3(1203.2445f, 4.4284425f, 310.8346f),
                new Vector3(1204.8328f, 4.555814f, 324.63104f),
                new Vector3(1201.2875f, 4.7499256f, 343.30676f),
                new Vector3(1194.5756f, 5.0008607f, 364.26294f),
                new Vector3(1186.5554f, 5.2968693f, 383.44565f),
                new Vector3(1178.1493f, 5.7230344f, 406.29797f),
                new Vector3(1175.0511f, 6.1372027f, 423.29047f),
                new Vector3(1172.0525f, 6.9665737f, 457.04575f),
                new Vector3(1169.6288f, 7.587892f, 488.3513f)
            ];

            foreach (var point in points)
            {
                Curve.AddPoint(point);
            }
        }
    }

}
