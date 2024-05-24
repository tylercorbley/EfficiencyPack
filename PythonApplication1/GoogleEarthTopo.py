import ee
import geopy
from geopy.geocoders import Photon

def process_address(address, size, resolution):
#def test():
    # Initialize the Earth Engine and geolocator
    ee.Initialize(project="ee-tylercorbley")
    geolocator = Photon(user_agent="geoapiExercises")
    
    # Geocode the address
    location = geolocator.geocode(address)
    #location = geolocator.geocode("426 naomi street philadelphia PA 19128")
    if not location:
        raise ValueError("Address not found")

    # Define the geometry around the address (e.g., a 1km x 1km square)
    buffer_size = size/100000 #0.005  # Approx. 500 meters in degrees
    geometry = ee.Geometry.Rectangle([
        location.longitude - buffer_size, location.latitude - buffer_size,
        location.longitude + buffer_size, location.latitude + buffer_size
    ])
    
    # Import the SRTM elevation dataset
    srtm = ee.Image('USGS/SRTMGL1_003')

    # Sample the elevation data within the polygon
    elevation_samples = srtm.sample(
        region=geometry,
        scale=resolution, #15-25 is good, 25+ if radius is big
        geometries=True
    )

     # Get the centroid of the geometry
    centroid = geometry.centroid(1)

    # Function to convert longitude and latitude to relative X and Y coordinates
    def convert_to_relative_coords(feature):
        geometry = feature.geometry()
        centroid = geometry.centroid()
        centroid_coords = centroid.coordinates()
    
        relative_x = ee.Number(location.longitude).subtract(ee.Number(centroid_coords.get(0))).multiply(111319.9)
        relative_y = ee.Number(location.latitude).subtract(ee.Number(centroid_coords.get(1))).multiply(111319.9)

        elevation = feature.get('elevation')
    
        return ee.Feature(geometry, {'relativeX': relative_x, 'relativeY': relative_y, 'elevation': elevation})

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
       # if relative_x is not None and relative_y is not None and elevation is not None:
        coordinates.append((relative_x, relative_y, elevation))
       # else:
        #    print("Skipping feature due to missing properties:", feature)
        #print(feature)

    print(coordinates)
    return coordinates

if __name__ == "__main__":
    #test()
    import sys
    address = sys.argv[1]
    process_address(address)
