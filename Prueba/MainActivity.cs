using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Hardware;
using Java.Lang;
using Java.Util;
using System.Collections.Generic;
using System.Linq;

namespace Prueba
{
    [Activity(Label = "Spike movimiento", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity , ISensorEventListener
    {
        static readonly object _syncLock = new object();
        private SensorManager _sensorManager;
        private TextView _sensorTextViewAX;
        private TextView _sensorTextViewAY;
        private TextView _sensorTextViewAZ;
        private TextView _sensorTextViewVX;
        private TextView _sensorTextViewVY;
        private TextView _sensorTextViewVZ;
        private TextView _sensorTextViewDX;
        private TextView _sensorTextViewDY;
        private TextView _sensorTextViewDZ;
        private TextView _filterValue;

        private TimeSpan _span;
        private DateTime _referencedTime;
        private DateTime _currentTime;

        private float _velX;
        private float _velY;
        private float _velZ;
        private float _disX;
        private float _disY;
        private float _disZ;

        private List<float> _deltaX = new List<float>();
        private List<float> _deltaY = new List<float>();
        private List<float> _deltaZ = new List<float>();

        private float x, y, z;

        private double _highPassFilter = 0;

        private bool _calibration = false;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);
            _sensorManager = (SensorManager)GetSystemService(Context.SensorService);
            _sensorTextViewAX = FindViewById<TextView>(Resource.Id.AcelX);
            _sensorTextViewAY = FindViewById<TextView>(Resource.Id.AcelY);
            _sensorTextViewAZ = FindViewById<TextView>(Resource.Id.AcelZ);
            _sensorTextViewVX = FindViewById<TextView>(Resource.Id.VelX);
            _sensorTextViewVY = FindViewById<TextView>(Resource.Id.VelY);
            _sensorTextViewVZ = FindViewById<TextView>(Resource.Id.VelZ);
            _sensorTextViewDX = FindViewById<TextView>(Resource.Id.PosX);
            _sensorTextViewDY = FindViewById<TextView>(Resource.Id.PosY);
            _sensorTextViewDZ = FindViewById<TextView>(Resource.Id.PosZ);
            _filterValue = FindViewById<TextView>(Resource.Id.FilterValue);

            _referencedTime = DateTime.Now;

            // Get our button from the layout resource,
            // and attach an event to it
            Button restart = FindViewById<Button>(Resource.Id.RestartButton);

            restart.Click += (object sender, EventArgs e) => {
                restartOnClick(sender, e);
            };

            Button calibration = FindViewById<Button>(Resource.Id.CalibrationButton);

            calibration.Click += (object sender, EventArgs e) => {
                calibrationOnClick(calibration, sender, e);
            };

            _deltaX.Add(0);
            _deltaY.Add(0);
            _deltaZ.Add(0);

            Button plus = FindViewById<Button>(Resource.Id.plusButton);

            plus.Click += (object sender, EventArgs e) => {
                plusOnClick(sender, e);
            };

            Button minus = FindViewById<Button>(Resource.Id.minusButton);

            minus.Click += (object sender, EventArgs e) => {
                minusOnClick(sender, e);
            };

            _filterValue.Text = string.Format("{0:f}", _highPassFilter);
        }

        protected override void OnResume()
        {
            base.OnResume();
            _sensorManager.RegisterListener(this, _sensorManager.GetDefaultSensor(SensorType.LinearAcceleration), SensorDelay.Fastest, 0); // El sensorDelay.Fastest es para que los eventos sean emitidos lo mas rápido posible. Implica mayor consumo de bateria.
        }

        protected override void OnPause()
        {
            base.OnPause();
            _sensorManager.UnregisterListener(this); //Debemos parar el sensor cuando se desactiva la aplicación, o podemos consumir enormemente la bateria del dispositivo
        }

        public void OnAccuracyChanged(Sensor sensor, SensorStatus accuracy)
        {
            // We don't want to do anything here.
        }

        public void OnSensorChanged(SensorEvent e)
        {
            lock (_syncLock)
            {
                _currentTime = DateTime.Now;
                _span = _currentTime - _referencedTime;
                _referencedTime = _currentTime;

                if (_calibration == true)
                {
                    _deltaX.Add(e.Values[0]);
                    _deltaY.Add(e.Values[1]);
                    _deltaZ.Add(e.Values[2]);
                }

                x = e.Values[0] - _deltaX.Average();
                y = e.Values[1] - _deltaY.Average();
                z = e.Values[2] - _deltaZ.Average();

                _sensorTextViewAX.Text = string.Format("Raw: {0:f}, Delta: {1:f}, Neto: {2:f}", e.Values[0], _deltaX.Average(), x);
                _sensorTextViewAY.Text = string.Format("Raw: {0:f}, Delta: {1:f}, Neto: {2:f}", e.Values[1], _deltaY.Average(), y);
                _sensorTextViewAZ.Text = string.Format("Raw: {0:f}, Delta: {1:f}, Neto: {2:f}", e.Values[2], _deltaZ.Average(), z);
                
                ChangeValues(_span.Milliseconds, x, y, z);
            }
        }

        public void ChangeValues(int t, float x, float y, float z)
        {
            if (Java.Lang.Math.Abs(x) > _highPassFilter)
            {
                _velX += x * ((float)t / 1000);
            }
            if (Java.Lang.Math.Abs(y) > _highPassFilter)
            {
                _velY += y * ((float)t / 1000);
            }

            if (Java.Lang.Math.Abs(z) > _highPassFilter)
            {
                _velZ += z * ((float)t / 1000);
            }
            
            _sensorTextViewVX.Text = string.Format("{0:f}", _velX);
            _sensorTextViewVY.Text = string.Format("{0:f}", _velY);
            _sensorTextViewVZ.Text = string.Format("{0:f}", _velZ);

            _disX += _velX * ((float)t / 1000);
            _disY += _velY * ((float)t / 1000);
            _disZ += _velZ * ((float)t / 1000);
            _sensorTextViewDX.Text = string.Format("{0:f}", _disX);
            _sensorTextViewDY.Text = string.Format("{0:f}", _disY);
            _sensorTextViewDZ.Text = string.Format("{0:f}", _disZ);
        }

        public void launchCalibration()
        {

        }

        public void restartOnClick(object sender, EventArgs e)
        {
            _disX = 0;
            _disY = 0;
            _disZ = 0;
            _velX = 0;
            _velY = 0;
            _velZ = 0;
        }

        public void calibrationOnClick(Button calibration, object sender, EventArgs e)
        {
            if (_calibration == false)
            {
                calibration.Text = "Calibration started";
                _calibration = true;
                _deltaX.Clear();
                _deltaY.Clear();
                _deltaZ.Clear();
                //  La lógica de esto es que fuese automatico y tardase un tiempo (5 seg?) en tomar la calibracion. 
                //  Pero para la prueba basica nos vale asi.
            }
            else
            {
                calibration.Text = "Calibration completed";
                _calibration = false;
            }
        }

        public void plusOnClick(object sender, EventArgs e)
        {
            _highPassFilter += 0.05;
            _filterValue.Text = string.Format("{0:f}", _highPassFilter);
        }

        public void minusOnClick(object sender, EventArgs e)
        {
            _highPassFilter -= 0.05;
            _filterValue.Text = string.Format("{0:f}", _highPassFilter);
        }
    }
}

