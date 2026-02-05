using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ZXing;
using ZXing.QrCode;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace Measure_UsingOwnCam
{
public partial class Form1 : Form
    {
        // 나중에 DLL로 추출할 핵심 필드
        private string _roomID;
        private string _relayServerUrl = "https://ornithopter83.github.io/Self-Tracking/"; 

        public Form1()
        {
            InitializeComponent();
            InitializePoseEngine();
        }

        private async void InitializePoseEngine()
        {
            // 1. 고유 룸 ID 생성 (스마트폰과 PC를 잇는 식별자)
            _roomID = Guid.NewGuid().ToString().Substring(0, 8);

            // 2. QR 코드 생성 및 표시
            string connectUrl = $"{_relayServerUrl}?room={_roomID}";
            pbQRCode.Image = GenerateQR(connectUrl);

            // 3. WebView2(수신 엔진) 초기화
            await wvReceiver.EnsureCoreWebView2Async(null);
            
            // 4. 웹 페이지 로드 (Room ID를 쿼리스트링으로 전달)
            wvReceiver.Source = new Uri(connectUrl);

            // 5. JavaScript와 C# 간의 통신 채널 설정
            wvReceiver.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
        }

        // [DLL 추출 가능 모듈: QR 생성]
        private Bitmap GenerateQR(string content)
        {
            var writer = new BarcodeWriter
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new QrCodeEncodingOptions
                {
                    Width = 250,
                    Height = 250,
                    Margin = 0
                }
            };
            return writer.Write(content);
        }

        // [DLL 추출 가능 모듈: 데이터 수신 핸들러]
        private void OnWebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            string jsonPayload = e.TryGetWebMessageAsString();
            
            // 여기서 수신된 JSON(관절 좌표)을 처리하거나 유니티로 넘길 이벤트를 발생시킵니다.
            // 예: ProcessPoseData(jsonPayload);
            Console.WriteLine($"수신된 좌표 데이터: {jsonPayload}");
        }
    }
}
