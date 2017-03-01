using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Timers;

public enum WheelShape
{
    Rectangle,
    Circle,
    BMPTexture,
}

public enum WheelOrientation
{
    Horizental,
    Vertical,
}

namespace VKnobCtrl
{
    [Serializable()]
    public partial class WheelControl : UserControl
    {
        private Color gradColor1;
        private Color gradColor2;
        private Color IndentColor;
        private Color backColor;
        private Color clrIndentTexTransparent;
        private WheelShape indentShape;
        private WheelOrientation ControlOrientation;
        private int IndentH;
        private int gradAlpha1;
        private int gradAlpha2;
        private float gradAngle;
        private float repeatNum;
        private int BorderThickness;
        private float MinRange;
        private float MaxRange;
        private float position;
        private float oldPosition;
        private float WheelTick;
        private bool _Hovering;
        private bool _LeftPressed;
        private bool bInertia;
        private int MouseDnPos;
        private int ControlLength;
        private int ControlWidth;
        private float RotNum;
        private bool bRedraw;
        LinearGradientBrush gradientBrush;
        private double DesiredRangeMin;
        private double DesiredRangeMax;
        private double DesiredPosition;
        private static System.Timers.Timer inertiaTimer;
        private int oldMousePosInertia;
        private double inStiffness;

        public delegate void PositionChanged(double pos);

        [field: NonSerialized()]
        public event PositionChanged OnWheelPositionChange;

        [field: NonSerialized()]
        private const double RefreshMilisec = 100.0;

        private Bitmap bmpIndentTex = new Bitmap(10, 10);
        private DateTime newTime;
        private DateTime oldTime;
        private double dMovingSpeed;


        public WheelControl()
        {
            InitializeComponent();
            DoubleBuffered = true;
            Rectangle ctrlRect = this.ClientRectangle;
            ControlLength = ctrlRect.Height;
            ControlWidth = ctrlRect.Width;
            
            gradColor1 = Color.Black;
            gradColor2 = Color.White;
            IndentColor = Color.Gray;
            backColor = Color.White;
            IndentH = 10;
            gradAlpha1 = 200;
            gradAlpha2 = 50;
            gradAngle = 90.0f;
            repeatNum = 10;
            BorderThickness = 5;
            MinRange = 0.0f;
            position = 0.0f;
            WheelTick = 100.0f;
            _Hovering = false;
            _LeftPressed = false;
            MouseDnPos = 0;
            RotNum = 100;
            MaxRange = (RotNum * NumWheelTick * repeatNum) + MinRange;
            bRedraw = true;
            gradientBrush = new LinearGradientBrush(ctrlRect, gradColor1, gradColor2, gradAngle, false);
            indentShape = WheelShape.Rectangle;
            ControlOrientation = WheelOrientation.Vertical;
            DesiredRangeMax = 10.0;
            DesiredRangeMin = -10.0;
            DesiredPosition = PosToDesiredPos();
            clrIndentTexTransparent = Color.White;
            bInertia = false;
            newTime = DateTime.Now;
            oldTime = DateTime.Now;
            dMovingSpeed = 0.0;
            oldMousePosInertia = 0;
            inStiffness = 0.98;
        }

        private bool Hovering
        {
            get
            {
                return (_Hovering);
            }
            set
            {
                _Hovering = value;
            }
        }

        private bool LeftMousePressed
        {
            get
            {
                return (_LeftPressed);
            }
            set
            {
                _LeftPressed = value;
            }
        }

        private int InitialMouseDownPosition
        {
            get
            {
                return (MouseDnPos);
            }
            set
            {
                MouseDnPos = value;
            }
        }

        [Category("Setup"), Description("Maximum number of wheel rotation")]
        public float NumWheelRotation
        {
            get
            {
                return (RotNum);
            }
            set
            {
                RotNum = value;
                MaxRange = (RotNum * NumWheelTick * repeatNum) + MinRange;
            }
        }

        [Category("Setup"), Description("Wheel position")]
        private float WheelPosition
        {
            get
            {
                return (position);
            }
            set
            {
                if (value == position)
                    return;

                if (value >= MaxRange)
                    position = MaxRange;
                else if (value <= MinRange)
                    position = MinRange;
                else
                    position = value;
                DesiredPosition = PosToDesiredPos(); 
                if (OnWheelPositionChange != null)
                    OnWheelPositionChange(DesiredWheelPosition);
                Invalidate();
            }
        }

        [Category("Setup"), Description("Turn on/off intertia on the wheel movement.")]
        public bool WheelInertia
        {
            get
            {
                return (bInertia);
            }
            set
            {
                bInertia = value;
            }
        }

        [Category("Setup"), Description("Wheel friction factor when WheelIntertia is on")]
        public double WheelInertiaFrictionFactor
        {
            get
            {
                return (inStiffness);
            }
            set
            {
                inStiffness = value;
                if (inStiffness > 1.0)
                    inStiffness = 1.0;
                else if (inStiffness < 0.0)
                    inStiffness = 0.0;
            }
        }

        [Category("Setup"), Description("Numbers between each wheel tick")]
        private float NumWheelTick
        {
            get
            {
                return (WheelTick);
            }
            set
            {
                WheelTick = value;
                MaxRange = (RotNum * NumWheelTick * repeatNum) + MinRange;
            }
        }

        [Category("Setup"), Description("Desired Range Minimum Value (Double)")]
        public double DesiredRangeMinimum
        {
            get
            {
                return (DesiredRangeMin);
            }
            set
            {
                DesiredRangeMin = value;
                DesiredPosition = PosToDesiredPos();
            }
        }

        [Category("Setup"), Description("Desired Range Maximum Value (Double)")]
        public double DesiredRangeMaximum
        {
            get
            {
                return (DesiredRangeMax);
            }
            set
            {
                DesiredRangeMax = value;
                DesiredPosition = PosToDesiredPos();
            }
        }

        [Category("Setup"), Description("Desired Value of the wheel in the range of DesiredRangeMinimum and DesiredRangeMaximum (Double)")]
        public double DesiredWheelPosition
        {
            get
            {
                return (DesiredPosition);
            }
            set
            {
                DesiredPosition = value;
                if (DesiredPosition >= DesiredRangeMax)
                    DesiredPosition = DesiredRangeMax;
                else if (DesiredPosition <= DesiredRangeMin)
                    DesiredPosition = DesiredRangeMin;
                WheelPosition = DesiredPosToPos();
                //Invalidate();
            }
        }


        [Category("Design"), Description("Number of repeats for texture")]
        public float RepeatNumber
        {
            get
            {
                return (repeatNum);
            }
            set
            {
                repeatNum = value;
                bRedraw = true;
                MaxRange = (RotNum * NumWheelTick * repeatNum) + MinRange;
                Invalidate();
            }
        }

         [Category("Design"), Description("Indentation Tickness")]
        public int IndentThickness
        {
            get
            {
                return (IndentH);
            }
            set
            {
                IndentH = value;
                bRedraw = true;
                Invalidate();               
            }
        }

        [Category("Design"), Description("Indentation Shape")]
        public WheelShape IndentShape
        {
            get
            {
                return (indentShape);
            }
            set
            {
                indentShape = value;
                bRedraw = true;
                Invalidate();
            }
        }

        [Category("Design"), Description("Orientation of the control wheel")]
        public WheelOrientation WheelOrientation
        {
            get
            {
                return (ControlOrientation);
            }
            set
            {
                ControlOrientation = value;
                if (ControlOrientation == WheelOrientation.Vertical)
                    gradAngle = 90.0f;
                else if (ControlOrientation == WheelOrientation.Horizental)
                    gradAngle = 0.0f;
                bRedraw = true;
                Invalidate();
            }
        }


        [Category("Design"), Description("Indentation bar Color")]
        public Color IndentationColor
        {
            get
            {
                return (IndentColor);
            }
            set
            {
                IndentColor = value;
                bRedraw = true;
                Invalidate();
            }
        }

        [Category("Design"), Description("Side Gradient Transparency")]
        public int AlphaGrad1
        {
            get
            {
                return (gradAlpha1);
            }
            set
            {
                gradAlpha1 = value;
                if (gradAlpha1 > 255)
                    gradAlpha1 = 255;
                else if (gradAlpha1 < 0)
                    gradAlpha1 = 0;
                bRedraw = true;
                Invalidate();
            }
        }

        [Category("Design"), Description("Middle Gradient Transparency")]
        public int AlphaGrad2
        {
            get
            {
                return (gradAlpha2);
            }
            set
            {
                gradAlpha2 = value;
                if (gradAlpha2 > 255)
                    gradAlpha2 = 255;
                else if (gradAlpha2 < 0)
                    gradAlpha2 = 0;
                bRedraw = true;
                Invalidate();
            }
        }
        [Category("Design"), Description("First Gradient Color")]
        public Color Gradient1
        {
            get
            {
                return (gradColor1);
            }
            set
            {
                gradColor1 = value;
                bRedraw = true;
                Invalidate();
            }
        }


        [Category("Design"), Description("Second Gradient Color")]
        public Color Gradient2
        {
            get
            {
                return (gradColor2);
            }
            set
            {
                gradColor2 = value;
                bRedraw = true;
                Invalidate();
            }
        }

        [Category("Design"), Description("Background Color")]
        public Color BackgroundColor
        {
            get
            {
                return (backColor);
            }
            set
            {
                backColor = value;
                bRedraw = true;
                Invalidate();
            }
        }

        [Category("Design"), Description("Thickness of border")]
        public int Border
        {
            get
            {
                return (BorderThickness);
            }
            set
            {
                BorderThickness = value;
                bRedraw = true;
                Invalidate();
            }
        }

        [Category("Design"), Description("Texture for indentation")]
        public Bitmap IndentTexture
        {
            get
            {
                return (bmpIndentTex);
            }
            set
            {
                bmpIndentTex = value;
                if(indentShape == WheelShape.BMPTexture)
                {
                    bmpIndentTex.MakeTransparent(clrIndentTexTransparent);
                    Invalidate();
                }
            }
        }

        [Category("Design"), Description("Set Transparent Color for the Indent Texture")]
        public Color IndentTextureTransparentColor
        {
            get
            {
                return (clrIndentTexTransparent);
            }
            set
            {
                clrIndentTexTransparent = value;
                if (indentShape == WheelShape.BMPTexture)
                {
                    bmpIndentTex.MakeTransparent(clrIndentTexTransparent);
                    Invalidate();
                }
            }
        }


        protected override void OnMouseMove(MouseEventArgs e)
        {
            //base.OnMouseMove(e);
            if(Hovering == false)
                Hovering = true;

            if(LeftMousePressed)
            {
                if(WheelOrientation == WheelOrientation.Vertical)
                {
                    position = CalcPosition(e.Y) + oldPosition;
                }
                else if (WheelOrientation == WheelOrientation.Horizental)
                {
                    position = CalcPosition(e.X) + oldPosition;
                }
                if (position <= MinRange)
                {
                    position = MinRange;
                }
                else if (position >= MaxRange)
                {
                    position = MaxRange;
                }
                if ((DateTime.Now - oldTime).TotalMilliseconds > RefreshMilisec)
                {
                    oldTime = DateTime.Now;
                    if (WheelOrientation == WheelOrientation.Vertical)
                        oldMousePosInertia = e.Y;
                    else if (WheelOrientation == WheelOrientation.Horizental)
                        oldMousePosInertia = e.X;
                }
                DesiredPosition = PosToDesiredPos();
                if (OnWheelPositionChange != null)
                    OnWheelPositionChange(DesiredWheelPosition);
                Invalidate();
            }

        }

        protected override void OnMouseLeave(EventArgs e)
        {
            //base.OnMouseLeave(e);
            Hovering = false;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            //base.OnMouseDown(e);
            if(e.Button == MouseButtons.Left)
            {
                LeftMousePressed = true;
                if (WheelOrientation == WheelOrientation.Vertical)
                {
                    InitialMouseDownPosition = e.Y;
                }
                else if (WheelOrientation == WheelOrientation.Horizental)
                {
                    InitialMouseDownPosition = e.X;
                }
                oldPosition = position;
                oldTime = DateTime.Now;
                newTime = DateTime.Now;
                oldMousePosInertia = InitialMouseDownPosition;
                if(inertiaTimer != null)
                {
                    inertiaTimer.Dispose();
                }
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            //base.OnMouseUp(e);
            if(e.Button == MouseButtons.Left)
            {
                LeftMousePressed = false;
                if (bInertia)
                {
                    newTime = DateTime.Now;
                    if ((newTime - oldTime).TotalMilliseconds > RefreshMilisec)
                    {
                        oldTime = newTime;
                        if (WheelOrientation == WheelOrientation.Vertical)
                            oldMousePosInertia = e.Y;
                        else if (WheelOrientation == WheelOrientation.Horizental)
                            oldMousePosInertia = e.X;
                    }

                    double tp = (newTime - oldTime).TotalMilliseconds;
                    if (tp == 0.0)
                        tp = 1.0;
                    if (WheelOrientation == WheelOrientation.Vertical)
                    {
                        dMovingSpeed = (double)CalcPosition(e.Y, oldMousePosInertia) / tp;
                    }
                    else if (WheelOrientation == WheelOrientation.Horizental)
                    {
                        dMovingSpeed = (double)CalcPosition(e.X, oldMousePosInertia) / tp;
                    }
                    inertiaTimer = new System.Timers.Timer(20);
                    inertiaTimer.Elapsed += OnInertiaTimerElapsed;
                    inertiaTimer.Enabled = true;
                }

            }
        }

        private void OnInertiaTimerElapsed(Object source, ElapsedEventArgs e)
        {
            DoInvoke(delegate {
                double tp = (e.SignalTime - newTime).TotalMilliseconds;
                WheelPosition += (float)(dMovingSpeed * tp);
                newTime = DateTime.Now;
                dMovingSpeed *= Math.Pow(WheelInertiaFrictionFactor, tp / 10.0);
                if (Math.Abs(dMovingSpeed) <= 0.1)
                    inertiaTimer.Dispose();
                Invalidate();
            });
            return;
        }

        private float CalcPosition(int MouseY)
        {
            return (CalcPosition(MouseY, InitialMouseDownPosition));
        }

        private float CalcPosition(int MouseY, int MouseOldY)
        {
            int res;
            float rg;
            res = MouseY - MouseOldY;
            rg = repeatNum * NumWheelTick;
            float cpos = 0.0f;

            if (WheelOrientation == WheelOrientation.Vertical)
                cpos = (((float)res * rg) / (float)ControlLength);
            else if (WheelOrientation == WheelOrientation.Horizental)
                cpos = (((float)res * rg) / (float)ControlWidth);
            return (cpos);
        }


        protected override void OnResize(EventArgs e)
        {
            Rectangle ctrlRect = this.ClientRectangle;
            ControlLength = ctrlRect.Height;
            ControlWidth = ctrlRect.Width;
            bRedraw = true;
            base.OnResize(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            //base.OnPaint(e);
            Rectangle ctrlRect = this.ClientRectangle;
            float theta;

            if (bRedraw)
            {
                UpdateGradientBrush();
                bRedraw = false;
            }

            theta = PosToTheta();
            while (theta >= (float)(Math.PI / -2.0))
            {
                theta -= ((float)Math.PI / RepeatNumber);
            }
            theta += ((float)Math.PI / RepeatNumber);
            

            using (SolidBrush bckgBrush = new SolidBrush(backColor))
            {
                e.Graphics.FillRectangle(bckgBrush, ctrlRect);
            }
            if (indentShape == WheelShape.Rectangle)
            {
                using (SolidBrush indentBrush = new SolidBrush(IndentColor))
                {
                    for (int i = 0; i <= RepeatNumber; i++)
                    {
                        Rectangle rct = IndentRect(theta, ctrlRect.Height, 0, ctrlRect.Width, IndentH);
                        e.Graphics.FillRectangle(indentBrush, rct);
                        theta += (float)Math.PI / RepeatNumber;
                        if (theta >= (float)(Math.PI / 2.0))
                            break;
                    }
                }
            }
            else if (indentShape == WheelShape.Circle)
            {
                for (int i = 0; i <= RepeatNumber; i++)
                {
                    Rectangle rct = IndentRect(theta, ctrlRect.Height, (ctrlRect.Width / 2) - (IndentH / 2), IndentH, IndentH);

                    GraphicsPath path = new GraphicsPath();
                    if (rct.Height < 1)  // Only works for Vertical. For Horizental needs another check for rect.Width < 1
                        path.AddEllipse(rct.X, rct.Y, rct.Width, 1);
                    else
                        path.AddEllipse(rct.X, rct.Y, rct.Width, rct.Height);

                    using (PathGradientBrush pthGrBrush = new PathGradientBrush(path))
                    {
                        pthGrBrush.CenterColor = Color.White;
                        Color[] colors = { IndentColor };
                        pthGrBrush.SurroundColors = colors;
                        e.Graphics.FillEllipse(pthGrBrush, rct);
                    }

                    theta += (float)Math.PI / RepeatNumber;
                    if (theta >= (float)(Math.PI / 2.0))
                        break;
                }
            }
            else if (indentShape == WheelShape.BMPTexture)
            {
                for (int i = 0; i <= RepeatNumber; i++)
                {
                    Rectangle rct = IndentRect(theta, ctrlRect.Height, 0, ctrlRect.Width, IndentH);
                    e.Graphics.DrawImage(bmpIndentTex, rct, 0, 0, bmpIndentTex.Width, bmpIndentTex.Height, GraphicsUnit.Pixel);
                    theta += (float)Math.PI / RepeatNumber;
                    if (theta >= (float)(Math.PI / 2.0))
                        break;
                }

            }



            e.Graphics.FillRectangle(gradientBrush, ctrlRect);
            using (Pen borderPen = new Pen(gradColor1, (float)BorderThickness))
            {
                if (BorderThickness != 0)
                    e.Graphics.DrawRectangle(borderPen, ctrlRect);
            }


            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
 
        }

        private Rectangle IndentRect(float angle, int Height, int xy, int Width, int inHeight)
        {
            double tp = Math.Sin((double)angle);
            float rH = (float)inHeight * (float)Math.Abs(Math.Cos((double)angle));
            float Rtop = 0.0f;
            Rectangle rct = new Rectangle();
            if(WheelOrientation == WheelOrientation.Vertical)
            {
                Rtop = (float)Height / 2.0f + (float)tp * (float)Height / 2.0f;
                Rtop -= rH / 2.0f;
                rct = new Rectangle(xy, (int)Rtop, Width, (int)rH);
            }
            else if (WheelOrientation == WheelOrientation.Horizental)
            {
                Rtop = (float)Width / 2.0f + (float)tp * (float)Width / 2.0f;
                Rtop -= rH / 2.0f;
                rct = new Rectangle((int)Rtop, xy, (int)rH, Height);
            }
            return (rct);
        }

        private float PosToTheta()
        {
            float ctrlL = 0.0f;
            if (WheelOrientation == WheelOrientation.Vertical)
                ctrlL = (float)ControlLength;
            else if (WheelOrientation == WheelOrientation.Horizental)
                ctrlL = (float)ControlWidth;

            float trans;
            float rgL, posL;
            rgL = NumWheelTick * repeatNum;
            posL = (position % rgL);
            trans = posL * ctrlL / rgL;

            return ((trans * (float)Math.PI / ctrlL) - (float)Math.PI / 2.0f);
        }

 
        private void UpdateGradientBrush()
        {
            Rectangle ctrlRect = this.ClientRectangle;
            gradientBrush = new LinearGradientBrush(ctrlRect, gradColor1, gradColor2, gradAngle, false);
            Color[] clr = {
                              Color.FromArgb(gradAlpha1, gradColor1.R, gradColor1.G, gradColor1.B),
                              Color.FromArgb(gradAlpha2, gradColor2.R, gradColor2.G, gradColor2.B),
                              Color.FromArgb(gradAlpha1, gradColor1.R, gradColor1.G, gradColor1.B),
                          };
            float[] RelPositions = { 0.0f, 0.5f, 1.0f };
            ColorBlend clrBlend = new ColorBlend();
            clrBlend.Colors = clr;
            clrBlend.Positions = RelPositions;
            gradientBrush.InterpolationColors = clrBlend;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            //base.OnPaintBackground(e);
        }

        private double PosToDesiredPos()
        {
            double res = 0.0;
            res = DesiredRangeMinimum + ((double)position - (double)MinRange) * (DesiredRangeMaximum - DesiredRangeMinimum) / ((double)MaxRange - (double)MinRange);
            return (res);
        }

        private float DesiredPosToPos()
        {
            float res = 0;
            res = MinRange + ((float)DesiredWheelPosition - (float)DesiredRangeMin) * (MaxRange - MinRange) / ((float)DesiredRangeMaximum - (float)DesiredRangeMinimum);
            return (res);
        }

        private void DoInvoke(MethodInvoker del)
        {
            if (InvokeRequired)
            {
                Invoke(del);
            }
            else
            {
                del();
            }
        }

    }
}
