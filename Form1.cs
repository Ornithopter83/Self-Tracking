using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json;
using ZXing;
using ZXing.QrCode;

namespace Measure_UsingOwnCam
{
    // --- 데이터 모델 ---
    public class PoseLandmark
    {
        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }
        public float visibility { get; set; }
    }

    public class PoseDataPayload
    {
        public string type { get; set; }
        public List<PoseLandmark> landmarks { get; set; }
        public int width { get; set; }
        public int height { get; set; }
    }

    // --- 투명 그리기용 커스텀 패널 ---
    public class TransparentPanel : Panel
    {
        public TransparentPanel()
        {
            // 투명도를 위한 스타일 설정
            this.SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            this.SetStyle(ControlStyles.Opaque, false);
            this.BackColor = Color.Transparent;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                // WS_EX_TRANSPARENT 스타일 적용하여 배경 통과
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x00000020; 
                return cp;
            }
        }
    }

    public partial class Form1 : Form
    {
        private string _roomID = Guid.NewGuid().ToString().Substring(0, 8);
        private string _baseUrl = "https://ornithopter83.github.io/Self-Tracking/";
        private List<PoseLandmark> _currentLandmarks = new List<PoseLandmark>();
        private TransparentPanel overlayPanel; // 투명 레이어

        public Form1()
        {
            InitializeComponent();
            SetupOverlay();
            this.Load += Form1_Load;
        }

        private void SetupOverlay()
        {
            overlayPanel = new TransparentPanel();
            overlayPanel.Paint += OverlayPanel_Paint;
            this.Controls.Add(overlayPanel);
            overlayPanel.BringToFront(); // 가장 앞으로
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            await InitializePoseEngine();
        }

        private async System.Threading.Tasks.Task InitializePoseEngine()
        {
            string mobileUrl = $"{_baseUrl.TrimEnd('/')}/index.html?room={_roomID}";
            string pcUrl = $"{mobileUrl}&mode=pc";

            if (pbQRCode != null) pbQRCode.Image = GenerateQR(mobileUrl);

            await wvReceiver.EnsureCoreWebView2Async(null);
            wvReceiver.CoreWebView2.PermissionRequested += (s, args) => args.State = CoreWebView2PermissionState.Allow;
            wvReceiver.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
            wvReceiver.CoreWebView2.NavigationCompleted += OnNavigationCompleted;

            wvReceiver.Source = new Uri(pcUrl);
        }

        private Bitmap GenerateQR(string content)
        {
            var writer = new BarcodeWriter
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new QrCodeEncodingOptions { Width = 150, Height = 150, Margin = 1 }
            };
            return writer.Write(content);
        }

        private async void OnNavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (!e.IsSuccess) return;

            await wvReceiver.CoreWebView2.ExecuteScriptAsync($"var CS_ROOM_ID = '{_roomID}'; var CS_IS_RECEIVER = true;");

            string poseLogic = @"
                (function() {
                    const peer = new Peer(CS_IS_RECEIVER ? CS_ROOM_ID : null, {
                        config: { 'iceServers': [{ url: 'stun:stun.l.google.com:19302' }] }
                    });
                    peer.on('open', id => statusElement.innerText = '연결대기: ' + id);

                    if (CS_IS_RECEIVER) {
                        peer.on('call', call => {
                            call.answer();
                            call.on('stream', stream => {
                                videoElement.srcObject = stream;
                                startMediaPipe();
                            });
                        });
                    }

                    function startMediaPipe() {
                        const pose = new Pose({
                            locateFile: (file) => `https://cdn.jsdelivr.net/npm/@mediapipe/pose/${file}`
                        });
                        pose.setOptions({ modelComplexity: 1, minDetectionConfidence: 0.5 });
                        pose.onResults(results => {
                            if (results.poseLandmarks) {
                                sendToCSharp({ type: 'POSE_DATA', landmarks: results.poseLandmarks });
                            }
                        });
                        videoElement.onplaying = () => {
                            sendToCSharp({ type: 'RESIZE_WINDOW', width: videoElement.videoWidth, height: videoElement.videoHeight });
                            const loop = async () => { await pose.send({ image: videoElement }); requestAnimationFrame(loop); };
                            loop();
                        };
                    }
                })();";

            await wvReceiver.CoreWebView2.ExecuteScriptAsync(poseLogic);
        }

        private void OnWebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                string json = e.TryGetWebMessageAsString();
                var data = JsonConvert.DeserializeObject<PoseDataPayload>(json);

                if (data.type == "RESIZE_WINDOW")
                {
                    this.BeginInvoke(new Action(() => {
                        this.ClientSize = new Size(data.width, data.height + 100);
                        wvReceiver.SetBounds(0, 0, data.width, data.height);
                        overlayPanel.SetBounds(0, 0, data.width, data.height);
                        this.CenterToScreen();
                    }));
                }
                else if (data.type == "POSE_DATA")
                {
                    _currentLandmarks = data.landmarks;
                    overlayPanel.Invalidate(); // 투명 패널만 다시 그리기
                }
            }
            catch { }
        }

        private void OverlayPanel_Paint(object sender, PaintEventArgs e)
        {
            if (_currentLandmarks == null || _currentLandmarks.Count < 13) return;

            Graphics g = e.Graphics;
            // 텍스트와 원이 부드럽게 그려지도록 설정
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            DrawMarker(g, _currentLandmarks[0], "HEAD", Color.Yellow);
            DrawMarker(g, _currentLandmarks[11], "L-SHOULDER", Color.Cyan);
            DrawMarker(g, _currentLandmarks[12], "R-SHOULDER", Color.Lime);
        }

        private void DrawMarker(Graphics g, PoseLandmark mark, string text, Color color)
        {
            if (mark.visibility < 0.5) return;

            float x = (1 - mark.x) * overlayPanel.Width;
            float y = mark.y * overlayPanel.Height;

            using (Brush b = new SolidBrush(color))
            using (Font f = new Font("Segoe UI", 12, FontStyle.Bold))
            {
                g.FillEllipse(b, x - 7, y - 7, 14, 14); // 점 크기 키움
                g.DrawString(text, f, b, x + 15, y - 10);
            }
        }
    }
}

// private string _baseUrl = "https://ornithopter83.github.io/Self-Tracking/"; 

//using System;
//using System.Collections.Generic;
//using System.Drawing;
//using System.Windows.Forms;
//using Microsoft.Web.WebView2.Core;
//using Newtonsoft.Json; // NuGet에서 Newtonsoft.Json 설치 필요
//using ZXing;
//using ZXing.QrCode;

//namespace Measure_UsingOwnCam
//{
//    public partial class Form1 : Form
//    {
//        private string _roomID;
//        // GitHub Pages에 배포된 본인의 주소를 입력하세요.


//        public Form1()
//        {
//            InitializeComponent();
//            this.Load += Form1_Load;
//        }

//        private async void Form1_Load(object sender, EventArgs e)
//        {
//            await InitializePoseEngine();
//        }

//        private async System.Threading.Tasks.Task InitializePoseEngine()
//        {
//            // 1. 고유 룸 ID 생성 (8자리 짧은 ID)
//            _roomID = Guid.NewGuid().ToString().Substring(0, 8);

//            // 2. URL 구성 (PC 모드 파라미터 포함)
//            string pcUrl = $"{_baseUrl.TrimEnd('/')}/index.html?room={_roomID}&mode=pc";
//            string mobileUrl = $"{_baseUrl.TrimEnd('/')}/index.html?room={_roomID}";

//            // 3. QR 코드 생성 (스마트폰 접속용)
//            pbQRCode.Image = GenerateQR(mobileUrl);

//            // 4. WebView2 초기화 및 권한 설정
//            await wvReceiver.EnsureCoreWebView2Async(null);
            
//            // 모든 브라우저 권한(카메라 등) 자동 허용 설정
//            wvReceiver.CoreWebView2.PermissionRequested += (s, args) => {
//                args.State = CoreWebView2PermissionState.Allow;
//            };

//            wvReceiver.CoreWebView2.Settings.IsWebMessageEnabled = true;
//            // 5. JavaScript 메시지 수신 이벤트 등록
//            wvReceiver.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

//            // 6. 페이지 로드
//            wvReceiver.Source = new Uri(pcUrl);
            
//            Console.WriteLine($"[PC URL]: {pcUrl}");
//            Console.WriteLine($"[Mobile URL]: {mobileUrl}");
//        }

//        private Bitmap GenerateQR(string content)
//        {
//            var writer = new BarcodeWriter
//            {
//                Format = BarcodeFormat.QR_CODE,
//                Options = new QrCodeEncodingOptions
//                {
//                    Width = pbQRCode.Width,
//                    Height = pbQRCode.Height,
//                    Margin = 0
//                }
//            };
//            return writer.Write(content);
//        }

//        // 핵심: 웹에서 보낸 관절 데이터를 처리하는 메서드
//        private void OnWebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
//        {
//            try
//            {
//                string json = e.TryGetWebMessageAsString();
//                dynamic data = JsonConvert.DeserializeObject(json);
//                string type = data.type;
        
//                if (type == "RESIZE_WINDOW")
//                {
//                    float vWidth = (float)data.width;
//                    float vHeight = (float)data.height;
        
//                    if (vWidth <= 0 || vHeight <= 0) return;
        
//                    this.BeginInvoke(new Action(() => {
//                        // 1. 현재 모니터의 작업 영역 크기 가져오기
//                        Rectangle screenRect = Screen.PrimaryScreen.WorkingArea;
//                        float maxWidth = screenRect.Width * 0.7f;  // 화면의 70%
//                        float maxHeight = screenRect.Height * 0.7f;
        
//                        // 2. 비율 유지하며 스케일 계산
//                        float scale = Math.Min(maxWidth / vWidth, maxHeight / vHeight);
                        
//                        // 만약 영상이 화면보다 작다면 확대하지 않고 원본 유지 (scale = 1.0)
//                        if (scale > 1.0f) scale = 1.0f;
        
//                        int targetWidth = (int)(vWidth * scale);
//                        int targetHeight = (int)(vHeight * scale);
        
//                        // 3. 최소 크기 보장 (에러 방지)
//                        targetWidth = Math.Max(targetWidth, 320);
//                        targetHeight = Math.Max(targetHeight, 240);
        
//                        // 4. 안전하게 적용
//                        this.ClientSize = new Size(targetWidth, targetHeight);
//                        wvReceiver.Size = this.ClientSize; // DockStyle이 Fill이 아닐 경우 대비
//                        this.CenterToScreen();
                        
//                        Console.WriteLine($"Resized to: {targetWidth}x{targetHeight} (Scale: {scale:F2})");
//                    }));
//                }
//                // ... POSE_DATA 처리
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine("Resize Error: " + ex.Message);
//            }
//        }
//    }
//    // 낱개 관절 좌표 정보
//    public class PoseLandmark
//    {
//        public float x { get; set; }
//        public float y { get; set; }
//        public float z { get; set; }
//        public float visibility { get; set; }
//    }

//    // 전체 데이터 묶음 (JSON의 루트 객체)
//    public class PoseDataPayload
//    {
//        public string type { get; set; }
//        public List<PoseLandmark> landmarks { get; set; }
//    }

//}