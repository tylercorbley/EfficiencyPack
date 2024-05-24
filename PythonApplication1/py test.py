#import ee
#from geopy.geocoders import Nominatim
from geopy.geocoders import Nominatim

try:
    import ee
    import geopy
except ImportError as e:
    print(f"Import error: {e}")
except Exception as e:
    print(f"An error occurred: {e}")

def test_geopy_and_ee():
   # ee.Authenticate(authorization_code=None, quiet=None, code_verifier=None, auth_mode=None)
    ee.Initialize(project="183131826638")
    geolocator = Nominatim(user_agent="geoapiExercises")
    location = geolocator.geocode("1600 Amphitheatre Parkway, Mountain View, CA")
    if location:
        print(f"Address: {location.address}")
        print(f"Latitude: {location.latitude}, Longitude: {location.longitude}")
    else:
        print("Address not found")

if __name__ == "__main__":
    test_geopy_and_ee()
