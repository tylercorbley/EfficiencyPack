import ee
import geopy
from geopy.geocoders import Photon
import sys
import json
import re

def dms_to_dd(dms_str):
    """
    Convert a DMS (degrees, minutes, seconds) string to decimal degrees.
    """
    dms_parts = re.split('[^\d\w]+', dms_str)
    degrees = float(dms_parts[0])
    minutes = float(dms_parts[1])
    seconds = float(dms_parts[2])
    direction = dms_parts[3]

    dd = degrees + (minutes/60.0) + (seconds/3600.0)

    if direction in ['S', 'W']:
        dd *= -1

    return dd

def get_coordinates(input_str, geolocator):
    """
    Detect if the input string is a pair of coordinates or an address.
    If it's an address, use the geolocator to find the coordinates.
    """
    # Regular expression patterns for detecting decimal degrees and DMS formats
    decimal_pattern = re.compile(r'^-?\d+\.\d+,\s*-?\d+\.\d+$')
    dms_pattern = re.compile(r'^\d+\u00B0\d+\'\d+\.\d+"[NSEW],\s*\d+\u00B0\d+\'\d+\.\d+"[NSEW]$')

    if decimal_pattern.match(input_str):
        lat, lon = map(float, input_str.split(','))
        return lat, lon
    elif dms_pattern.match(input_str):
        lat_dms, lon_dms = input_str.split(',')
        lat = dms_to_dd(lat_dms.strip())
        lon = dms_to_dd(lon_dms.strip())
        return lat, lon
    else:
        # Assume it's an address and use geolocator
        location = geolocator.geocode(input_str)
        if not location:
            raise ValueError("Address not found")
        return location.latitude, location.longitude

def get_elevation_data(input_str, size, scale):
    # Initialize the Earth Engine and geolocator
    ee.Initialize(project="ee-tylercorbley")
    geolocator = Photon(user_agent="geoapiExercises")

    # Get coordinates from the input string
    latitude, longitude = get_coordinates(input_str, geolocator)
    
    buffer_size = float(size)

    # Calculate latitude and longitude buffers in degrees
    latitudeBuffer = float(buffer_size / 364000)
    longitudeBuffer = float(buffer_size / 288200)
    
    geometry = ee.Geometry.Rectangle([
        longitude - longitudeBuffer, latitude - latitudeBuffer,
        longitude + longitudeBuffer, latitude + latitudeBuffer
    ])

    # Import the SRTM elevation dataset
    srtm = ee.Image('USGS/SRTMGL1_003')
    
    # Sample the elevation data within the polygon
    elevation_samples = srtm.sample(
        region=geometry,
        numPixels=float(scale),
        scale=float(30),
        geometries=True
    )

    # Function to convert longitude and latitude to relative X and Y coordinates
    def convert_to_relative_coords(feature):
        geom = feature.geometry()
        centroid = geom.centroid()
        centroid_coords = centroid.coordinates()

        relative_x = ee.Number(centroid_coords.get(0)).subtract(longitude).multiply(111319.9)
        relative_y = ee.Number(centroid_coords.get(1)).subtract(latitude).multiply(111319.9)

        elevation = feature.get('elevation')

        return ee.Feature(geom, {'relativeX': relative_x, 'relativeY': relative_y, 'elevation': elevation})

    # Apply the conversion function to the samples
    relative_coords = elevation_samples.map(convert_to_relative_coords)

    # Get the coordinates from the server
    relative_coords_list = relative_coords.getInfo()

    # Extract the coordinates into a list of tuples
    coordinates = []
    for feature in relative_coords_list['features']:
        properties = feature.get('properties', {})
        relative_x = properties.get('relativeX')
        relative_y = properties.get('relativeY')
        elevation = properties.get('elevation')
        coordinates.append((relative_x, relative_y, elevation))

    return coordinates

if __name__ == "__main__":
    #ee.Authenticate(force=True)
    if len(sys.argv) != 4:
        print(json.dumps({"error": "Invalid number of arguments"}))
        sys.exit(1)

    address = sys.argv[1]
    size = sys.argv[2]
    scale = sys.argv[3]

    try:
        coordinates = get_elevation_data(address, size, scale)
        print(json.dumps(coordinates))
    except Exception as e:
        print(json.dumps({"error": str(e)}))
