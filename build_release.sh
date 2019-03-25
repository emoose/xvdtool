if [ ! $# -eq 3 ]; then
  echo "Usage: ./build_release [RID] [TAG] [TARGET FRAMEWORK]"
  echo "Example:"
  echo "RID: for example: win-x64, win-x86, linux-x64, osx.10.12-x64"
  echo "TAG: A TAG to include in the release archive filename"
  echo "TARGET FRAMEWORK: for example: netcoreapp2.0"
  exit 1
fi

PUBLISH_TARGET=$1
TAG_NAME=$2
TARGET_FRAMEWORK=$3
PROJ_ROOT=$(pwd)
PUBLISH_ARGS="/p:TrimUnusedDependencies=true"

# Cleanup first
dotnet clean

# Build core library
rm -fR LibXboxOne/publish-$PUBLISH_TARGET
dotnet publish $PUBLISH_ARGS -c Release -f netstandard2.0 -r $PUBLISH_TARGET -o publish-$PUBLISH_TARGET LibXboxOne

# Publish builds
mkdir -p release/$PUBLISH_TARGET
rm -fR release/$PUBLISH_TARGET/*
for proj in {XVDTool,XBFSTool,DurangoKeyExtractor}
do
    rm -fR $proj/publish-$PUBLISH_TARGET
    dotnet publish $PUBLISH_ARGS -c Release -f $TARGET_FRAMEWORK -r $PUBLISH_TARGET -o publish-$PUBLISH_TARGET $proj
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
