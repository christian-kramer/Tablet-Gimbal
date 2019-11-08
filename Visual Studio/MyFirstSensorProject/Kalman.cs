namespace MyFirstSensorProject
{
    class Kalman
    {
        public float estimate;
        public float estimate_error;
        public float measurement;
        public float measurement_error;

        private float previous_estimate;
        private float previous_estimate_error;
        private float kalman_gain;

        public Kalman(float initial_estimate, float initial_estimate_error, float initial_measurement_error)
        {
            estimate = initial_estimate;
            estimate_error = initial_estimate_error;
            measurement_error = initial_measurement_error;
        }

        public float filter(float this_measurement)
        {
            kalman_gain = estimate_error / (estimate_error + measurement_error);

            previous_estimate = estimate;
            estimate = previous_estimate + (kalman_gain * (this_measurement - previous_estimate));

            previous_estimate_error = estimate_error;
            estimate_error = (1 - kalman_gain) * previous_estimate_error;

            return estimate;
        }
    }
}
