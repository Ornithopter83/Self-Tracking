
namespace Measure_UsingOwnCam
{
	partial class Form1
	{
		/// <summary>
		/// 필수 디자이너 변수입니다.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// 사용 중인 모든 리소스를 정리합니다.
		/// </summary>
		/// <param name="disposing">관리되는 리소스를 삭제해야 하면 true이고, 그렇지 않으면 false입니다.</param>
		protected override void Dispose (bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form 디자이너에서 생성한 코드

		/// <summary>
		/// 디자이너 지원에 필요한 메서드입니다. 
		/// 이 메서드의 내용을 코드 편집기로 수정하지 마세요.
		/// </summary>
		private void InitializeComponent ()
		{
			this.pbQRCode = new System.Windows.Forms.PictureBox();
			this.pbVideoPreview = new System.Windows.Forms.PictureBox();
			this.wvReceiver = new Microsoft.Web.WebView2.WinForms.WebView2();
			((System.ComponentModel.ISupportInitialize)(this.pbQRCode)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.pbVideoPreview)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.wvReceiver)).BeginInit();
			this.SuspendLayout();
			// 
			// pbQRCode
			// 
			this.pbQRCode.Location = new System.Drawing.Point(681, 243);
			this.pbQRCode.Name = "pbQRCode";
			this.pbQRCode.Size = new System.Drawing.Size(250, 250);
			this.pbQRCode.TabIndex = 0;
			this.pbQRCode.TabStop = false;
			// 
			// pbVideoPreview
			// 
			this.pbVideoPreview.Location = new System.Drawing.Point(13, 13);
			this.pbVideoPreview.Name = "pbVideoPreview";
			this.pbVideoPreview.Size = new System.Drawing.Size(640, 480);
			this.pbVideoPreview.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
			this.pbVideoPreview.TabIndex = 1;
			this.pbVideoPreview.TabStop = false;
			// 
			// wvReceiver
			// 
			this.wvReceiver.AllowExternalDrop = true;
			this.wvReceiver.CreationProperties = null;
			this.wvReceiver.DefaultBackgroundColor = System.Drawing.Color.White;
			this.wvReceiver.Location = new System.Drawing.Point(708, 13);
			this.wvReceiver.Name = "wvReceiver";
			this.wvReceiver.Size = new System.Drawing.Size(171, 149);
			this.wvReceiver.TabIndex = 2;
			this.wvReceiver.ZoomFactor = 1D;
			// 
			// Form1
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(951, 509);
			this.Controls.Add(this.wvReceiver);
			this.Controls.Add(this.pbVideoPreview);
			this.Controls.Add(this.pbQRCode);
			this.Name = "Form1";
			this.Text = "Form1";
			((System.ComponentModel.ISupportInitialize)(this.pbQRCode)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.pbVideoPreview)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.wvReceiver)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.PictureBox pbQRCode;
		private System.Windows.Forms.PictureBox pbVideoPreview;
		private Microsoft.Web.WebView2.WinForms.WebView2 wvReceiver;
	}
}

