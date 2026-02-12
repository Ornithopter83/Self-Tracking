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
            // 1. 가장 먼저 원시 JSON 문자열을 가져옵니다.
            string json = e.WebMessageAsJson; // WebMessageAsJson 속성을 사용해 보세요.
        
            // 2. 창 제목에 일단 찍어봅니다 (데이터가 들어오는지 확인)
            this.Invoke(new Action(() => {
                this.Text = $"데이터 수신 시간: {DateTime.Now:HH:mm:ss}";
            }));
        
            try
            {
                // 3. 파싱 시도
                var poseData = JsonConvert.DeserializeObject<PoseDataPayload>(json);
                if (poseData != null && poseData.landmarks != null)
                {
                    // 성공 시 로직
                    var nose = poseData.landmarks[0];
                    Console.WriteLine($"Nose Y: {nose.y}");
                }
            }
            catch (Exception ex)
            {
                // 파싱 에러가 있다면 여기에 찍힙니다.
                Console.WriteLine("JSON 파싱 에러: " + ex.Message);
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