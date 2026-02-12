using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json; // NuGet에서 Newtonsoft.Json 설치 필요
using ZXing;
using ZXing.QrCode;

namespace Measure_UsingOwnCam
{
    public partial class Form1 : Form
    {
        private string _roomID;
        // GitHub Pages에 배포된 본인의 주소를 입력하세요.
        private string _baseUrl = "https://ornithopter83.github.io/Self-Tracking/"; 

        public Form1()
        {
            InitializeComponent();
            this.Load += Form1_Load;
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            await InitializePoseEngine();
        }

        private async System.Threading.Tasks.Task InitializePoseEngine()
        {
            // 1. 고유 룸 ID 생성 (8자리 짧은 ID)
            _roomID = Guid.NewGuid().ToString().Substring(0, 8);

            // 2. URL 구성 (PC 모드 파라미터 포함)
            string pcUrl = $"{_baseUrl.TrimEnd('/')}/index.html?room={_roomID}&mode=pc";
            string mobileUrl = $"{_baseUrl.TrimEnd('/')}/index.html?room={_roomID}";

            // 3. QR 코드 생성 (스마트폰 접속용)
            pbQRCode.Image = GenerateQR(mobileUrl);

            // 4. WebView2 초기화 및 권한 설정
            await wvReceiver.EnsureCoreWebView2Async(null);
            
            // 모든 브라우저 권한(카메라 등) 자동 허용 설정
            wvReceiver.CoreWebView2.PermissionRequested += (s, args) => {
                args.State = CoreWebView2PermissionState.Allow;
            };

            wvReceiver.CoreWebView2.Settings.IsWebMessageEnabled = true;
            // 5. JavaScript 메시지 수신 이벤트 등록
            wvReceiver.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

            // 6. 페이지 로드
            wvReceiver.Source = new Uri(pcUrl);
            
            Console.WriteLine($"[PC URL]: {pcUrl}");
            Console.WriteLine($"[Mobile URL]: {mobileUrl}");
        }

        private Bitmap GenerateQR(string content)
        {
            var writer = new BarcodeWriter
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new QrCodeEncodingOptions
                {
                    Width = pbQRCode.Width,
                    Height = pbQRCode.Height,
                    Margin = 0
                }
            };
            return writer.Write(content);
        }

        // 핵심: 웹에서 보낸 관절 데이터를 처리하는 메서드
        private void OnWebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                string json = e.TryGetWebMessageAsString();
                
                // JSON 데이터를 C# 객체로 파싱
                var poseData = JsonConvert.DeserializeObject<PoseDataPayload>(json);

                if (poseData?.type == "POSE_DATA")
                {
                    // 여기서 관절 데이터를 활용합니다.
                    // 예: 0번 관절(코)의 X좌표 확인
                    var nose = poseData.landmarks[0];
                    
                    // UI에 좌표 정보를 아주 간단히 출력해봅니다 (성능 확인용)
                    this.BeginInvoke(new Action(() => {
                        this.Text = $"Nose: X={nose.x:F2}, Y={nose.y:F2}";
                    }));

                    // [추후 계획] 이 데이터를 Unity DLL의 이벤트로 전달함
                    // OnPoseUpdated?.Invoke(poseData.landmarks);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Data Parsing Error: " + ex.Message);
            }
        }
    }
    // 낱개 관절 좌표 정보
    public class PoseLandmark
    {
        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }
        public float visibility { get; set; }
    }

    // 전체 데이터 묶음 (JSON의 루트 객체)
    public class PoseDataPayload
    {
        public string type { get; set; }
        public List<PoseLandmark> landmarks { get; set; }
    }

}