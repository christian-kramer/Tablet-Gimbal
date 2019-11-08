namespace MyFirstSensorProject
{
    class Kalman
    {
        public double estimate;
        public double estimate_error;
        public double measurement_error;

        private double previous_estimate;
        private double previous_estimate_error;
        private double kalman_gain;

        public Kalman(double initial_estimate, double initial_estimate_error, double initial_measurement_error)
        {
            estimate = initial_estimate;
            estimate_error = initial_estimate_error;
            measurement_error = initial_measurement_error;
        }

        public double filter(double this_measurement)
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
