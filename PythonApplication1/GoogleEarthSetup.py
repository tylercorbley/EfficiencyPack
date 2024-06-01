import ee
import geopy
import sys
from geopy.geocoders import Photon

def main():
    ee.Authenticate(force=True)
    # Initialize the Earth Engine and geolocator
    ee.Initialize(project="ee-tylercorbley")
    pass

if __name__ == "__main__":
    sys.exit(int(main() or 0))
    
