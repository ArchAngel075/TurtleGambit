function __printHeader()
    term.clear()
    term.setCursorPos(1,1)
    print("===================")
    print("===TURTLE GAMBIT===")
    print("===================")
end
__printHeader();
print("Booting....Please wait...");
os.sleep(1.5);

local __version__ = "turtle-brain-pull";

if(	not fs.exists("_version") and fs.exists("disk/_version") ) then
    print("Version copied from disk")
    os.sleep(1.5)
    fs.copy("disk/_version","_version")
end

if fs.exists("_version") then 
    os.sleep(1.5)
    local f = fs.open("_version","r");
    __version__ = f.readAll()
    print("Version Loaded From Disk.")
    f.close()
end
print("Version [" .. __version__.."]")

local env = getfenv().arg[0];
if env == "disk/startup.lua" then
    print("Turtle detected Gambit running from a disk. Will attempt to install Turtle Brain using version ["..__version__.."]")
    os.sleep(1.5);
    local url = "https://raw.githubusercontent.com/ArchAngel075/TurtleGambit/"..__version__.."/Assets/TurtleBrain/startup.lua"
    local fetch = http.get(url);
    local brainFile = fs.open("startup.lua","w");
    brainFile.write(fetch.readAll())
    brainFile.flush()
    brainFile.close()
    print("Brain downloaded successfully.")
    os.sleep(1.5)

    --copy _address and _server files
    if(	not fs.exists("_address.lua") and fs.exists("disk/_address.lua") ) then
        print("Address copied from disk")
        os.sleep(1.5)
        fs.copy("disk/_address.lua","_address.lua")
    end
    
    if(	not fs.exists("_server.lua") and fs.exists("disk/_server.lua") ) then
        print("Server name copied from disk")
        os.sleep(1.5)
        fs.copy("disk/_server.lua","_server.lua")
    end
    --attempt to resolve server and address :
    if(not fs.exists("_server.lua")) then
        local serverName = false
        while not serverName do
            __printHeader()
            print("In order to function reliably the turtle requires the server name the Mission Control app is targeting.")
            print("This would be found by the name of the server selected when the Mission Control app is started.")
            print("Please provide server name in a file named [_server.lua] OR alternatively please enter the name now > ");
            serverName = read();
        end
        local f = fs.open("_server.lua","w")
        f.write("return \"" .. serverName .. "\"")
        f.close()
    end
    
    if(not fs.exists("_address.lua")) then
        local addr = false;
        while not addr do
            __printHeader()
            print("In order to function reliably the turtle requires the host address the Mission Control app is exposed on.")
            print("This would be found by the address given by the ngrok approach, localhost or (DANGEROUS) your public ip.")
            print("Please provide the address in a file named [_address.lua] OR alternatively enter the address now >");
            addr= read();
        end
        local f = fs.open("_address.lua","w")
        f.write("return \"" .. addr .. "\"")
        f.close()
    end
    
else
    --this should run IF startup is actually not on disk :
    if not fs.exists("brain.lua") then
        __printHeader()
        print("The turtle brain is either outdated or is missing. I will attempt to fetch it now...");
        local url = "https://raw.githubusercontent.com/ArchAngel075/TurtleGambit/"..__version__.."/Assets/TurtleBrain/brain.lua"
        local fetch = http.get(url);
        local brainFile = fs.open("brain.lua","w");
        brainFile.write(fetch.readAll())
        brainFile.flush()
        brainFile.close()
        print("Turtle Brain received successfully...");
        os.sleep(1.5)
    end

    if not fs.exists("json.lua") then
        __printHeader()
        print("The turtle needs JSON functionality. I will attempt to fetch now...");
        local url = "https://raw.githubusercontent.com/ArchAngel075/TurtleGambit/"..__version__.."/Assets/TurtleBrain/json.lua"
        local fetch = http.get(url);
        local brainFile = fs.open("json.lua","w");
        brainFile.write(fetch.readAll())
        brainFile.flush()
        brainFile.close()
        print("Turtle JSON module received successfully...");
        os.sleep(1.5)
    end
    print("Loading Brain...");
    os.sleep(1.5)
    require("brain")
end







