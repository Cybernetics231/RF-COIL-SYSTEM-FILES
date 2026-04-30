import math

# ==========================
# Defined Parameters
# ==========================
flip_angle_deg   = 90.0             # Flip angle in degrees
pulse_duration_s = 1.0/1000.0       # RF pulse duration in seconds
gamma_MHz_T      = 42.58            # Gyromagnetic ratio in MHz/T (hydrogen default)
coil_radius_m    = 0.273/2.0        # Coil radius in meters
turns            = 15.0             # Number of turns in the coil
voltage_factor   = 50.0             # Assumed load impedance (50 Ω)

# Input signal parameters
B_Field_from_magnets = 50.0/1000.0  # 50 mT

########################################################################
# Functions
########################################################################

def calculate_B1(flip_angle_deg, pulse_duration_s, gamma_MHz_T):
    theta = math.radians(flip_angle_deg)                  # convert to radians
    gamma = gamma_MHz_T * 1e6 * 2 * math.pi               # rad/s/T
    return theta / (gamma * pulse_duration_s)

# Biot-Savart approximation for on-axis B1
def calculate_current(B1, radius_m, turns):
    mu0 = 4 * math.pi * 1e-7  
    return (2 * radius_m * B1) / (mu0 * turns)

# V_rms = I * R (assuming matched 50 Ω load)
def calculate_voltage(I, factor):
    return I * factor

def larmor_freq(B_Field_from_magnets, gamma_MHz_T):
    freq = gamma_MHz_T * 1e6 * B_Field_from_magnets
    return freq

# ==========================
# Run Core Calculations
# ==========================
B1 = calculate_B1(flip_angle_deg, pulse_duration_s, gamma_MHz_T)
I  = calculate_current(B1, coil_radius_m, turns)
Vo_rms = calculate_voltage(I, voltage_factor)
Larmor_freq = larmor_freq(B_Field_from_magnets, gamma_MHz_T) / 1e6

# ==========================
# Amplifier Gain Parameters
# ==========================
gain = 63.38                    # Voltage gain in dB (change to 64.0 if desired)

# Gain calculations
gain_ratio = gain / 20.0
Vin_rms = Vo_rms / (10.0 ** gain_ratio)

# Vin peak-to-peak for sine wave
Vin_peak_to_peak = Vin_rms * 2 * math.sqrt(2)

# Power delivered to 50 Ω load
Power_avg_mW = (Vo_rms ** 2 / 50.0) * 1000.0

# ==========================
# Coupler + Oscilloscope Calculations  (This was the main fix)
# ==========================
coupler_attenuation_db = -50.0

# Correct voltage attenuation factor for -50 dB
attenuation_factor = 10 ** (coupler_attenuation_db / 20.0)

V_scope_rms = Vo_rms * attenuation_factor

# Proper peak-to-peak voltage on the scope (sine wave)
V_scope_vpp = V_scope_rms * 2 * math.sqrt(2)

# ==========================
# Display All Results
# ==========================
print("=== Preliminary Calculations ===")
print(f"B1 field:          {B1:.3e} T")
print(f"Current required:  {I:.4f} A")
print(f"Vo_rms (amplifier output): {Vo_rms:.4f} V")
#print(f"Larmor Frequency:  {Larmor_freq:.3f} MHz")
print("Larmor Frequency: 2.18 MHz")
print("========================================")

print("\n=== Amplifier Gain & Input ===")
print(f"Target Gain:       {gain:.2f} dB")
print(f"Vin_rms:           {Vin_rms*1000:.3f} mV")
print(f"Vin peak-to-peak:  {Vin_peak_to_peak*1000:.3f} mV")
print(f"Power at output:   {Power_avg_mW:.2f} mW")
print("========================================")

print("\n=== Oscilloscope After -50 dB Coupler ===")
print(f"Attenuation factor: 1 / {1/attenuation_factor:.1f}")
print(f"V_scope_rms:       {V_scope_rms*1000:.3f} mV")
print(f"Vpp on scope:      {V_scope_vpp*1000:.3f} mV")
print(f"Measured_Value: 32.732 mV")
print(f"Difference:        {V_scope_vpp*1000 - 32.732:.1f} mV ({((V_scope_vpp*1000 - 32.732)/32.732)*100:+.1f}%)")
print("========================================")

print("\n=== Final Drive Waveform ===")
print(f"V_in(t) = {Vin_peak_to_peak*1000:.3f} * sin(2pi({Larmor_freq:.3f}*10^6t)Hz)  mV")
