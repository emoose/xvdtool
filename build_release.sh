if [ ! $# -eq 2 ]; then
  echo "Usage: ./build_release [RID] [TAG]"
  echo "Example:"
  echo "RID: for example: win-x64, win-x86, linux-x64, osx.10.12-x64"
  echo "TAG: A TAG to include in the release archive filename"
  exit 1
fi

PUBLISH_TARGET=$1
TAG_NAME=$2
PROJ_ROOT=$(pwd)

# Publish builds
mkdir -p release/$PUBLISH_TARGET
for proj in {XVDTool,XBFSTool,DurangoKeyExtractor}
do
    dotnet publish -c Release -r $PUBLISH_TARGET -o publish-$PUBLISH_TARGET $proj
    cp -R $proj/publish-$PUBLISH_TARGET/* release/$PUBLISH_TARGET/
done

# Bundle additional files
cp README.md release/$PUBLISH_TARGET/
cp CHANGELOG.md release/$PUBLISH_TARGET/

# Package up
cd release/$PUBLISH_TARGET
zip -r ../XVDTool-$PUBLISH_TARGET-$TAG_NAME.zip .

# Cleanup
cd $PROJ_ROOT
rm -fR release/$PUBLISH_TARGET
