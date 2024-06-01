import ee
import geopy
from geopy.geocoders import Photon
import sys
import json
import math

def get_elevation_data(address, size, scale):
    # Initialize the Earth Engine and geolocator
    ee.Initialize(project="ee-tylercorbley")
    geolocator = Photon(user_agent="geoapiExercises")
    
    # Geocode the address
    location = geolocator.geocode(address)
    if not location:
        raise ValueError("Address not found")
    buffer_size = float(size)

    # Calculate latitude and longitude buffers in degrees
    latitudeBuffer = float(buffer_size/364000)
    longitudeBuffer = float(buffer_size/288200)
    
    a1 = float(location.longitude - longitudeBuffer)
    a2 = float(location.latitude - latitudeBuffer)
    a3 = float(location.longitude + longitudeBuffer)
    a4 = float(location.latitude + latitudeBuffer)
    
    geometry = ee.Geometry.Rectangle([
        a1,a2,
        a3,a4
    ])
# Import the SRTM elevation dataset
    srtm = ee.Image('USGS/SRTMGL1_003')
    
    # Sample the elevation data within the polygon
    elevation_samples = srtm.sample(
        region=geometry,
        numPixels=float(scale),
       scale = float(30),
        geometries=True
    )

   # Function to convert longitude and latitude to relative X and Y coordinates
    def convert_to_relative_coords(feature):
        geom = feature.geometry()
        centroid = geom.centroid()
        centroid_coords = centroid.coordinates()

        relative_x = ee.Number(centroid_coords.get(0)).subtract(location.longitude).multiply(111319.9)
        relative_y = ee.Number(centroid_coords.get(1)).subtract(location.latitude).multiply(111319.9)


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