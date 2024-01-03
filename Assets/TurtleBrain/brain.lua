--TODO : downloader for zlib and sha256...
local zip = require("zlib")
json = require("./json");
settings.load();
local function __printHeader()
    term.clear()
    term.setCursorPos(1,1)
    print("===================")
    print("===TURTLE GAMBIT===")
    print("===================")
    --print("Drivers:");
end
function log(...)
    print('>',table.unpack({...}));
    if(fs.getSize("latest.log") > 167693) then fs.delete("latest.log");end
    local f = fs.open("latest.log","a")
    local message = {...};
    local writ = "> ";
    for k,v in pairs(message or {}) do message[k] = tostring(v); 
        writ = writ .. "\t" .. tostring(v)
    end
    writ = writ .. "\n";
    f.write(writ)
    f.flush();
    -- if type(json) == "table" then message = json.encode(message) else
    --     message = table.concat(message or {},"\t,") or "";
    -- end
    -- message = ">".. tostring(message) .. "\n";
    -- f.write(message)
    f.close()
    if(sock) then
        --report(message);
    end
end

function logWithoutReport(...)
    print(...);
    if(fs.getSize("latest.log") > 167693) then fs.delete("latest.log"); end
    local f = fs.open("latest.log","a")
    local message = {...};
    for k,v in pairs(message) do message[k] = tostring(v) end
    message = ">".. tostring(table.concat(message,"\t")) .. "\n";
    f.write(message)
    f.close()
end

function report(message)
	local output = message
    if(sock) then
	    --send(output,"LOG");
    else
        logWithoutReport("[ERROR] Unable to send report. No connection present! Message as follows:",message)
    end
end

if fs.exists("latest.log") then fs.delete("latest.log") end
local f = fs.open("latest.log","w")
f.write("{Begin}\n");
f.close()
f = nil;

local url
local f = fs.open("_address","r")
url = f.readAll();
f.close()
local server
local f = fs.open("_server","r")
server = f.readAll();
f.close()

sock = false;

function send(data,type)
    -- logWithoutReport("sending",type or "MESSAGE")
    parallel.waitForAny(function() sock.send(json.encode({turtleId=os.getComputerID(),Type=type or 2,Data=json.encode(data)})) end);
end

function sendRaw(data)
    logWithoutReport("sending raw",data)
    parallel.waitForAny(function() sock.send(data) end);
end

function report(message)
	local output = message
	send(output,"LOG");
end

--place the proxy chest and return a wrap to it.
--return false and reason if fails.
function placeProxyChest()
    local sideToPlace = getFreeSide()
    local _itemSlot = findItem({prefix="minecraft",suffix="chest"});
    if _itemSlot then log("chest found") else log("chest not found"); return false,nil, "no chest" end
    if not sideToPlace then return false,nil, "no unoccupied side to utilise!" end
    turtle.select(_itemSlot);
    place(sideToPlace);
    os.pullEvent("peripheral");
    local wrap_side = sideToSide(sideToPlace);
    local proxy_chest = wrap(sideToPlace);
    settings.define("proxy_side" , {Type="string"});
    settings.set("proxy_side", sideToPlace);
    settings.save();
    return proxy_chest, sideToPlace, nil
end

--attempt to pick up the proxy chest on the last known side.
--return false and reason if fails.
function takeProxyChest()
    settings.load();
    local proxy_side = settings.get("proxy_side");
    log("proxy_side is [" .. tostring(proxy_side).. "]" )
    if not proxy_side then return false, "no saved position of proxy chest!" end
    if not detect(proxy_side) then
        return false, "nothing was found on the side " .. tostring(proxy_side);
    end
    local is,details = inspect(proxy_side)
    --confirm its a chest!
    if details.name ~= "minecraft:chest" then
        return false, "block on side " .. tostring(proxy_side) .. " was not a chest!"
    end
    dig(proxy_side);
    os.pullEvent("peripheral_detach");
    return true
end

function take(sideFrom, slotFrom, count, slotTo)
    proxy_chest,proxySide,reason = placeProxyChest();
    if not proxy_chest then report(reason); return false end
    turtle.select(tonumber(slotTo));
    proxy_chest.pullItems(peripheralSafeSide(sideFrom), tonumber(slotFrom), tonumber(count)) --pull to slot 1;
    suck(proxySide);
    takeProxyChest();
    helpers.observeInventory()
    helpers.observeExternalInventory(sideFrom);
end

function moveExternal(side, slotFrom, count, slotTo)
    log("move external on side",side,"and from slot",slotfrom, "to", slotTo, "the count of",count)
    local pss = peripheralSafeSide(side)
    local inventory = peripheral.wrap(pss);
    log("inventory wrapped on peripheralSafeSide from",side,"to",pss,"yielded",inventory)
    if not inventory then
        report("unable to wrap as peripheral on side",side)
        return
    end
    if not inventory.pullItems then
        report("unable to pull items, no method 'pullItems' found on wrapped peripheral on side",side)
        return;
    end
    log("attempt to pull->")
    local a,b,c = inventory.pullItems(pss, tonumber(slotFrom), tonumber(count), tonumber(slotTo)) --pull to slot 1;
    log("result of attempt to pull",a,b,c);
    helpers.observeExternalInventory(side);
end

function put(sideTo, slotFrom, count, slotTo)
    proxy_chest,proxySide,reason = placeProxyChest();
    if not proxy_chest then report(reason); return false end
    turtle.select(tonumber(slotFrom));
    drop(proxySide, tonumber(count)); --move to proxy chest those items
    proxy_chest.pushItems(peripheralSafeSide(sideTo), 1, tonumber(count), tonumber(slotTo)) --pull to slot 1;
    takeProxyChest();
    helpers.observeInventory()
    helpers.observeExternalInventory(peripheralSafeSide(sideTo));
end

function wrap(side)
    local side = string.lower(side);
    if(side == "down" or side == "bottom") then return peripheral.wrap("bottom") end
    if(side == "" or side == "front") then return peripheral.wrap("Front") end
    if(side == "up" or side == "top") then return peripheral.wrap("Top") end
end

function dig(side)
    local side = string.lower(side or "");
    if(side == "down" or side == "bottom") then return turtle.digDown() end
    if(side == "" or side == "front") then return turtle.dig() end
    if(side == "up" or side == "top") then return turtle.digUp() end
end

function place(side)
    local side = string.lower(side or "");
    if(side == "down" or side == "bottom") then return turtle.placeDown() end
    if(side == "" or side == "front") then return turtle.place() end
    if(side == "up" or side == "top") then return turtle.placeUp() end
end

function suck(side, count)
    local side = string.lower(side or "");
    if(side == "down" or side == "bottom") then return turtle.suckDown(count) end
    if(side == "" or side == "front") then return turtle.suck(count) end
    if(side == "up" or side == "top") then return turtle.suckUp(count) end
end

function drop(side,count)
    local side = string.lower(side or "");
    if(side == "down" or side == "bottom") then return turtle.dropDown(count) end
    if(side == "" or side == "front") then return turtle.drop(count) end
    if(side == "up" or side == "top") then return turtle.dropUp(count) end
end

function detect(side)
    local side = string.lower(side or "");
    if(side == "down" or side == "bottom") then return turtle.detectDown() end
    if(side == "" or side == "front") then return turtle.detect() end
    if(side == "up" or side == "top") then return turtle.detectUp() end
end

function inspect(side)
    local side = string.lower(side or "");
    local action = "";
    if(side == "down" or side == "bottom") then action = "inspectDown"  end
    if(side == "" or side == "front") then action = "inspect" end
    if(side == "up" or side == "top") then action = "inspectUp" end
    local a,b = turtle[action]()
    return a,b
end

function peripheralSafeSide(side)
    local side = string.lower(side);
    if(side == "down" or side == "bottom") then side = "bottom" end
    if(side == "" or side == "front") then side = "front" end
    if(side == "up" or side == "top") then side = "top" end
    return side;
end

--this should eventually be replaced with a injected script gist.
function sideToSide(side)
    if side == "Down" then return "Bottom" end
    if side == "Up" then return "Top" end
    if side == "Top" then return "Up" end
    if side == "" then return "Front" end
    if side == "Front" then return "" end
    return false;
end

function listCount(list)
    local count = 0
    log("..list Type",type(list));
    if(type(list) ~= "table") then return 0 end
    for k,v in pairs(list) do count = count + 1 end
    return count;
end

function getFreeSide()
    return not turtle.detect() and "" or not turtle.detectDown() and "Down" or not turtle.detectUp() and "Up";
end

function findItem(search)
    for i = 1,16 do
        local slot = turtle.getItemDetail(i)
        if(slot) then
            if(type(search) == "string" and slot.name == search) then
                return i
            elseif(type(search) == "table") then
                local prefix = not search.prefix or (search.prefix and #slot.name >= #search.prefix and string.sub(slot.name,0,#search.prefix) == search.prefix)
                local suffix = not search.suffix or (search.suffix and #slot.name >= #search.suffix and string.sub(slot.name,-#search.suffix) == search.suffix)
                if(prefix and suffix) then
                    return i
                end
            end
        end
    end
    return false;
end

function getItemDetail(slot,detailed)
    local detail = turtle.getItemDetail(slot,detailed)
    if(not detail) then 
        detail = {
            name = "GAMBIT:NONE",
            amount = -1;
        }
    end
    return detail;
end

function request(id, data, typehint, f_reply)
    log("sending a request and awaiting some response")
    send({id=id,args=json.encode(data), typehint=typehint or "NONE"},"REQUEST")
    _awaiting_ = true;
    _on_reply_ = f_reply or function() end
    local response = {false, "no reason given"};
    function await_reply()
        while true do
            local e = {os.pullEvent()}
            log("grab event")

            if(e[1] == "websocket_message") then
                log("grab event was a websocket_message")
                local status,data = pcall(function() return json.decode(e[3]) end)
                if(status) then
                    log("decoded","msg:", e[3])
                    log("data.Type:",data.Type)
                    if(data.Type == "reply") then
                        log("grab event was a websocket_message and a reply")
                        local reply = json.decode(data["message"]);
                        if(reply.to == id) then
                            log("reply.to:",reply.to,"match",reply.to and reply.to == id)
                            log("and it is the reply we wanted!")
                            _awaiting_ = false;
                            parallel.waitForAny(function () response = {_on_reply_(json.decode(reply.reply))} end)
                            break;
                        end
                    end
                end
            end
        end
    end
    parallel.waitForAny(await_reply);
    report("response is " .. json.encode(response));
    return unpack(response);
end

function processMsg(msg)
    log("msg",msg)
    local status,data = pcall(function() return json.decode(msg) end)
    --print("status",status)
    if(status) then
        if(data.Type == "EVALUATE") then
            log("evaluating ",data.message)
            if fs.exists("_eval.lua") then fs.delete("_eval.lua") end
            local infile = fs.open("_eval.lua","w")
            infile.write(data.Data);
            infile.flush()
            infile.close()
            f,err = load(data.Data,nil,"t",_ENV);
            if not f then 
                log("something broke >\n" .. tostring(err)) 
            else 
                out,response = pcall(f); 
                if response then 
                    log(response)
                end 
            end
        elseif(data.Type == "MultiPartMessageHeader") then
            log()
            log("MultiPartMessageHeader received.",data.Expected, data.GUID)
            local parts_count = tonumber(data.Expected);
            settings.set("parts_expected", parts_count);
            settings.define("parts_received" , {Type="table"});
            settings.set("parts_guid", data.GUID)
            settings.set("parts_received", {});
            settings.save();
            -- send(nil,"HEADER_ACK");
        elseif(data.Type == "MultiPartMessagePart") then
            log(data.Type,data.PartNum,data.Data)
            local parts = settings.get("parts_received");
            table.insert(parts,data.PartNum);
            settings.set("parts_received", parts);
            settings.save();
            if(fs.exists("_part_" .. tostring(data.PartNum))) then fs.delete("_part_" .. tostring(data.PartNum)) end
            log("space remaining... " .. fs.getFreeSpace("") .. " need " .. (#data.Data))
            local f = fs.open("_part_" .. tostring(data.PartNum) ,"w");
            f.write(data.Data);
            f.flush()
            f.close()
        elseif(data.Type == "MultiPartMessageConfirmationRequest") then
            settings.load();
            local parts_received = settings.get("parts_received");
            local parts_expected = settings.get("parts_expected");
            local to_resend = {};
            local any_missing = false;
            for i = 1,parts_expected do
                local missing = true;
                log("is part '" .. tostring(i) .. "' missing>");
                for o = 1,#parts_received do
                    if(i == o) then
                        log("\tpart '" .. tostring(i) .. "' is not missing!");
                        missing = false;
                    end
                end
                if(missing) then
                    log("\tpart '" .. tostring(i) .. "' is missing!");
                    any_missing = true;
                    table.insert(to_resend,i)
                end
            end
            if(any_missing) then
                log("there are missing parts!");
                send({parts=to_resend, GUID=settings.get("parts_guid")},"MultiPartMessagePleaseSend");
            else
                send(nil,"MultiPartMessageOK")
            end
        elseif(data.Type == "MultiPartMessageExecute") then
            log("executing accumulated message");
            settings.load();
            local parts_received = settings.get("parts_received");
            local compiled = ""
            for k,v in pairs(parts_received) do
                log("load in '" .. "_part_" .. tostring(v) .. "'");
                assert(fs.exists("_part_" .. tostring(v)),"file not found " .. "_part_" .. tostring(v));
                local f = fs.open("_part_" .. tostring(v) ,"r");
                compiled = compiled .. f.readAll();
                f.close()
                fs.delete("_part_" .. tostring(v));
            end

            if fs.exists("_eval.lua") then fs.delete("_eval.lua") end
            local infile = fs.open("_eval.lua","w")
            infile.write(compiled);
            infile.flush()
            infile.close()

            settings.set("parts_expected", 0);
            settings.set("parts_received", {});
            settings.save()
            log("process compiled...");
            log("__message__");
            log(compiled)
            log("-----------")
            local outcome = {processMsg(compiled)}
            log("execution completed. send unlock")
            send(nil,"MultiPartMessageExecuted")
            report(outcome);
            log("--DONE--");
        elseif(data.Type == "procedure") then
            log("procedure received.");
            f,err = load(data.Data,nil,"t",_ENV);
            if not f then 
                report("something went wrong >\n" .. tostring(err)) 
            else
                out,response = pcall(f); 
                if response then 
                    report(response)
                end 
                log("procedure definition on", tostring(response))
                procedure_f = response --actually executes the procedure logic, which should define and return an function.
                log("procedure running...");
                parallel.waitForAll(procedure);
                log("procedure done...");
            end
        elseif(data.Type == "raw") then
            log("server>",data.Data)
        end
    end
end

local procedure_f = nil;
local _awaiting_ = false;
local _on_reply_;
function procedure()
    _awaiting_ = false;

    function websocket_aborts()
        while true do
            local e = {os.pullEvent()}
            if(e[1] == "websocket_message") then
                local status,data = pcall(function() return json.decode(e[3]) end)
                if(status) then
                    if(data.Type == "abort") then
                        _awaiting_ = false;
                        break;
                    end
                end
            end
        end
    end

    parallel.waitForAny(procedure_f, websocket_aborts);

    return;
end

function message_dequeue()
    while true do
        __printHeader();
        local event, murl, message
        -- print("Fetch next event...")
        event, murl, message = os.pullEvent()
        if event == "speaker_audio_empty" then
            if #bufferings > 0 then
                local pop = table.remove(bufferings,1);
                local speaker = peripheral.find("speaker")
                speaker.playAudio(pop)
            else
                report("Speach completed.")
            end
        elseif event == "websocket_message" then
            log(event, murl, message)
            message = zip:DecompressDeflate(message);
            log("Process Message:",message);
            if message == "__PING__" then
                sendRaw("__PONG__")
                helpers.observeFuelLevel();
            else
                processMsg(message);
            end
        elseif event == 'websocket_closed' then 
            os.reboot() 
        elseif event == 'key' then 
            
        elseif event == 'turtle_inventory' then
            helpers.observeInventory()
        end
    end
    log("END MESSAGE EXPECTATION")
end

--base sixty four encoding helper :
-- Base64 encoding function
function base64_encode(data)
    local b='ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/'
    return ((data:gsub('.', function(x) 
        local r,b='',x:byte()
        for i=8,1,-1 do r=r..(b%2^i-b%2^(i-1)>0 and '1' or '0') end
        return r;
    end)..'0000'):gsub('%d%d%d?%d?%d?%d?', function(x)
        if (#x < 6) then return '' end
        local c=0
        for i=1,6 do c=c+(x:sub(i,i)=='1' and 2^(6-i) or 0) end
        return b:sub(c+1,c+1)
    end)..({ '', '==', '=' })[#data%3+1])
end

-- Base64 decoding function
function base64_decode(data)
    local b='ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/'
    data = string.gsub(data, '[^'..b..'=]', '')
    return (data:gsub('.', function(x)
        if (x == '=') then return '' end
        local r,f='',(b:find(x)-1)
        for i=6,1,-1 do r=r..(f%2^i-f%2^(i-1)>0 and '1' or '0') end
        return r;
    end):gsub('%d%d%d?%d?%d?%d?%d?%d?', function(x)
        if (#x ~= 8) then return '' end
        local c=0
        for i=1,8 do c=c+(x:sub(i,i)=='1' and 2^(8-i) or 0) end
        return string.char(c)
    end))
end

------------------------
---HELPERS
------------------------
helpers = {empty="GAMBIT:EMPTY"};
Drivers = {};
helpers.RebuildDrivers = function() 
    log("Rebuilding Drivers...");
    Drivers = {};
    local DriversList = fs.list("Drivers")
    for k,path in pairs(DriversList) do
        if(fs.isDir("Drivers/" .. path) and fs.exists("Drivers/"..path.."/driver.lua")) then
            log("Attempt to load driver","Drivers/"..path.."/driver");
            Drivers[path] = require("Drivers/"..path.."/driver");
            log("Loaded driver ",path,"at","Drivers/"..path.."/driver");
        end
    end 
    log("Drivers Rebuilt...");
    for k,driverName in pairs(Drivers) do print("#",k) end
end

helpers.forward = function() 
    local try, reason = turtle.forward();
    if try then
        helpers.observeMyLocation("forward");
    else
        report(reason)
    end
    if(peripheral.find("universal_scanner")) then
        helpers.observeUPW();
    else
        helpers.observeFront();
        helpers.observeDown();
        helpers.observeUp();
    end
end
helpers.back = function() 
    local try,reason = turtle.back();
    if try then
        helpers.observeMyLocation("back");
    else
        report(reason)
    end
    if(peripheral.find("universal_scanner")) then
        helpers.observeUPW();
    else
        helpers.observeFront();
        helpers.observeDown();
        helpers.observeUp();
    end
end
helpers.up = function() 
    local try, reason = turtle.up();
    if try then
        helpers.observeMyLocation("up");
    else
        report(reason)
    end
    if(peripheral.find("universal_scanner")) then
        helpers.observeUPW();
    else
        helpers.observeFront();
        helpers.observeDown();
        helpers.observeUp();
    end
end
helpers.down= function() 
    local try, reason = turtle.down();
    if try then
        helpers.observeMyLocation("down");
    else
        report(reason)
    end
    if(peripheral.find("universal_scanner")) then
        helpers.observeUPW();
    else
        helpers.observeFront();
        helpers.observeDown();
        helpers.observeUp();
    end
end
helpers.turnLeft = function() 
    turtle.turnLeft();
    helpers.observeMyDirection('LEFT');
    if(peripheral.find("universal_scanner")) then
        helpers.observeUPW();
    else
        helpers.observeFront();
    end
end
helpers.turnRight = function() 
    turtle.turnRight();
    helpers.observeMyDirection('RIGHT');
    if(peripheral.find("universal_scanner")) then
        helpers.observeUPW();
    else
        helpers.observeFront();
    end
end

helpers.observeUPW = function()
    local scanner = peripheral.find("universal_scanner");
    if scanner == nil or scanner == false then return end
    --we have access to the powerful universal scanner
    local scan = scanner.scan("block",1);
    for k,block in pairs(scan) do
        log("SCAN:",block.name,block.x,block.y,block.z)
        local payload = {details=block, ObservationType="UPW_SCAN"};
        send(payload,'Observation');
    end
end

helpers.observeFront = function()
    air,inspection = turtle.inspect();
    if not air then 
        inspection = { name = 'GAMBIT:AIR' }
    end
    payload = { details=inspection, ObservationType='BLOCK_FORWARD' };
    send(payload,'Observation')
end
helpers.observeDown = function()
    air,inspection = turtle.inspectDown();
    if not air then 
        inspection = { name = 'GAMBIT:AIR' }
    end
    payload = { details=inspection, ObservationType='BLOCK_DOWN' };
    send(payload,'Observation')
end
helpers.observeUp = function()
    air,inspection = turtle.inspectUp();
    if not air then 
        inspection = { name = 'GAMBIT:AIR' }
    end
    payload = { details=inspection, ObservationType='BLOCK_UP' };
    send(payload,'Observation')
end
helpers.observeAll = function()
    helpers.observeMyLocation("none");
    helpers.observeDown();
    helpers.observeUp();
    turtle.turnRight(); helpers.observeFront();
    turtle.turnRight(); helpers.observeFront();
    turtle.turnRight(); helpers.observeFront();
    turtle.turnRight(); helpers.observeFront();
end

helpers.use = function()
    turtle.use();
    helpers.observeFront();
    helpers.observeInventory();
end
helpers.useDown = function()
    turtle.useDown();
    helpers.observeDown();
    helpers.observeInventory();
end
helpers.useUp = function()
    turtle.useUp();
    helpers.observeUp();
    helpers.observeInventory();
end

helpers.observeMyLocation = function(change)
    local payload;
    local x,y,z = gps.locate();
    if not x then
        log("gps locate failed. using assumption");
        if change then
            payload = { change=change , ObservationType='SELF_ASSUME' };
        else
            payload = { change="none" , ObservationType='SELF_ASSUME' };
            log("unable to assume location!");
            return
        end
    else
        payload = { x=x,y=y,z=z, ObservationType='SELF' };
    end
    send(payload,'Observation')
end
helpers.observeMyDirection = function(dir)
    payload = { change=dir, ObservationType='ROTATION' };
    send(payload,'Observation')
end

helpers.getSlotDetails = function(slot)
    slot = slot or turtle.getSelectedSlot()
    payload = turtle.getItemDetail(slot) or {name='GAMBIT:EMPTY',count=0};
    payload.slot=slot
    payload.ObservationType='SLOT_DETAIL'
    send(payload,'Observation')
end

helpers.getSelectedSlot = function(slot)
    slot = slot or turtle.getSelectedSlot()
    payload = {}
    payload.slot=slot
    payload.ObservationType='SLOT_SELECTED'
    send(payload,'Observation')
end

helpers.select = function(a)
    turtle.select(a)
    helpers.getSelectedSlot()
    helpers.getSlotDetails()
end

function helpers.observeExternalInventory(side)
    local wrap = peripheral.wrap(peripheralSafeSide(side));
    if wrap and wrap.list and wrap.size then
        listing = wrap.list();
        local items = {}
        for k,v in pairs(listing) do table.insert(items, {slot=k,count=v.count,name=v.name}) end
        local payload = {items=items, size = wrap.size()}
        payload.ObservationType='EXTERNAL_INVENTORY'
        payload.side = side
        send(payload,'Observation')
    elseif wrap and wrap.items then
        report("modded inventory");
        listing = wrap.items();
        local items = {}
        for k,v in pairs(listing) do table.insert(items, {slot=k,count=v.count,name=v.name}) end
        local payload = {items=items, size = #items}
        payload.ObservationType='EXTERNAL_INVENTORY'
        payload.side = side
        send(payload,'Observation')
    else
        report("unable to fetch inventory at side " .. tostring(side) .. ". Maybe it is modded?");
    end
end

function helpers.observeInventory()
    log("i will observe inventory");
    local payload = {ObservationType="INVENTORY", slots = {}}
    for i = 1,16 do 
        local slotData = {}
        slotData.data = turtle.getItemDetail(i) or {name='GAMBIT:EMPTY',count=0};
        slotData.index = i;
        table.insert(payload.slots,slotData);
    end
    send(payload,'Observation')
end

function helpers.place()
    turtle.place()
    helpers.observeFront();
    helpers.observeInventory();
end

function helpers.placeDown()
    turtle.placeDown()
    helpers.observeDown();
    helpers.observeInventory();
end

function helpers.placeUp()
    turtle.placeUp()
    helpers.observeUp();
    helpers.observeInventory();
end
--dig
function helpers.dig()
    turtle.dig()
    helpers.observeFront();
    helpers.observeInventory();
end

function helpers.digDown()
    turtle.digDown()
    helpers.observeDown();
    helpers.observeInventory();
end

function helpers.digUp()
    turtle.digUp()
    helpers.observeUp();
    helpers.observeInventory();
end
--attack
function helpers.attack()
    turtle.attack()
    helpers.observeFront();
end

function helpers.attackDown()
    turtle.attackDown()
    helpers.observeDown();
end

function helpers.attackUp()
    turtle.attackUp()
    helpers.observeUp();
end
--drop
function helpers.drop()
    turtle.drop()
    helpers.observeFront();
    helpers.observeInventory();
end

function helpers.dropDown()
    turtle.dropDown()
    helpers.observeDown();
    helpers.observeInventory();
end

function helpers.dropUp()
    turtle.dropUp()
    helpers.observeUp();
    helpers.observeInventory();
end
--inventory:
function helpers.transferTo(from,to,count)
    turtle.select(from)
    turtle.transferTo(to, count or nil)
    helpers.observeInventory();
end

function helpers.refuel()
    turtle.refuel(1)
    helpers.observeFuelLevel()
end

function helpers.reboot()
    os.reboot()
end

function helpers.equipLeft()
    turtle.equipLeft()
    helpers.observeInventory();
end

function helpers.equipRight()
    turtle.equipRight()
    helpers.observeInventory();
end

function helpers.craft()
--ths is involved.
    --first find and mark the crafting table.
        --attempt to equip the table.
        --mark what equiupment we took off - if any
        --find and place proxy chest.
            --empty into chest using select and drop those slots outside of craft range
            --attempt to craft.
            --suck from proxy until empty,
            --pickup proxy
            --find the equipment (if any) we swapped and equip
    local slotsToEmpty = {4,8,12,13,14,15,16}
    local swapWith = false;
    log("find crafting table")
    local craftSlot = findItem({prefix="minecraft",suffix="crafting_table"});
    if not craftSlot then log("No crafting table. cancelling"); return end
    log("found crafting table")
    log("place chest")
    local proxy,proxy_side,proxy_fail_reason = placeProxyChest()
    if not proxy then log(proxy_fail_reason); return false end
    helpers.observeInventory();
    log("chest placed")
    log("equip crafting table")
    turtle.select(craftSlot);
    local equipped = turtle.equipLeft();
    helpers.observeInventory();
    if equipped then 
        log("equipped crafting table")
        local swappable = turtle.getItemDetail();
        if swappable then
            swapWith = swappable.name
            log("note to swap back to ", swapWith);
        end
        log("equipped crafting table")
        log("prepare inventory")
        for k,v in pairs(slotsToEmpty) do
            turtle.select(v)
            drop(proxy_side)
        end
        helpers.observeInventory();
        turtle.select(craftSlot)
        drop(proxy_side);
        log("prepared inventory")
        log("craft 1")
        turtle.craft(1);
        helpers.observeInventory();
        log("crafted 1")
        log("reclaim items")
        for i = 1,27 do
            suck(proxy_side)
        end
        helpers.observeInventory();
        log("items reclaimed")
        log("reclaim proxy")
        takeProxyChest();
        log("proxy reclaimed")
        if(swappable) then
            log("try return equipment")
            local returnEquipSlot = findItem(swapWith);
            if returnEquipSlot then 
                turtle.select(returnEquipSlot)
                turtle.equipLeft();
                log("equipment swapped back")
            else
                log("failed to re equip what i had before") 
            end
        end
        log("finish up by reporting my inventory")
        helpers.observeInventory();
    else
        log("failed to equip table.")
    end
    log("crafting DONE.")
end
--fuel:
function helpers.observeFuelLevel()
    local i = turtle.getFuelLevel()
    local l = turtle.getFuelLimit()
    local payload = {ObservationType="FUEL",level=i,limit=l}
    send(payload,'Observation')
end

--a note on how (non text) files are managed.
--generally I will strive to "put" files onto a turtle with their content as byte64 encoded.
--the decodeFile command shall be used to futher then recode the data to its original form.
--this preserves to the best ability the content of the file from its BYTEs than string content.
--this is generally applied to non plain text files, such as audio (dfpwm)

function helpers.proxyOnfile(filename,mode,method,data)
    method = method or "write";
    local f = fs.open(filename,mode or "w");
    f[method](data);
    f.flush()
    f.close()
end

--decode a file by assuming its contents are base64 encoded.
--rewrites to the file in place.
function helpers.decodeFile(filename)
    log("decoding file",filename)
    local f = fs.open(filename,"r");
    local encodedData = f.readAll();
    f.close()
    fs.delete(filename);
    local f = fs.open(filename,"wb");
    local decodedData = base64_decode(encodedData);
    f.write(decodedData)
    f.flush()
    f.close()
    log("file decoded",filename)
end

bufferings = {}
decoder = nil
function helpers.playAudio(phrase)
    log("playing audio-table.")
    if not (type(phrase) == "table") then log("ERROR","Phrase must be a table!"); return end
    bufferings = {};
    local speaker = peripheral.find("speaker")
    if not speaker then log("no speaker attached") end
    local dfpwm = require("cc.audio.dfpwm")
    decoder = dfpwm.make_decoder()
    for k,chunk in pairs(phrase) do
        -- assert(#chunk <= 16 * 1024, "phrase chunk must be 16*1024")
        local buffer = decoder(chunk)
        bufferings[#bufferings+1] = buffer;
    end
    if #bufferings > 0 then
        local pop = table.remove(bufferings,1);
        speaker.playAudio(pop)
        return true;
    end
    return false
end

function helpers.playFile(file)
    log("Playing audio from file ",file)
    if not fs.exists(file) then log("file not found ",file) end
    local speaker = peripheral.find("speaker")
    if not speaker then log("no speaker attached") end
    local dfpwm = require("cc.audio.dfpwm")
    decoder = dfpwm.make_decoder()

    --read file then play :
    for chunk in io.lines(file, 16 * 1024) do
        local buffer = decoder(chunk)
        bufferings[#bufferings+1] = buffer;
    end
    
    if #bufferings > 0 then
        local pop = table.remove(bufferings,1);
        speaker.playAudio(pop)
        return true;
    end
    return false
end

------------------------
parallel.waitForAny(function()
    while true do
        __printHeader();
        settings.load()
        local gambitprotocol = settings.get("gambitprotocol") or "wss";
        settings.set("gambitprotocol",gambitprotocol)
        log("protocol in use is",gambitprotocol);
        local fullAddress = (gambitprotocol .. "://" .. url .."/" .. server);
        log("try to connect to server @ ["..fullAddress.."]");
        sock,reason = http.websocket(fullAddress)
        if(sock) then
            log("Connected. Sending handshake.");
            local event, murl, message
            event, murl, message = os.pullEvent("websocket_message")
            if message == "HELLO GAMBIT" then 
                log("Handshake successful."); 
                break 
            else 
                log("Incorrect handshake",message," rebooting.");
                os.sleep(1.5)
                os.reboot()
            end
        else
            if(string.find(reason,"getStatus: 501 Not Implemented") or string.find(reason,"Message is too large")) then
                local t = 5;
                while true do
                    t = t - 1
                    __printHeader();
                    log("No such server accepting for given server name [" .. server .. "]","\nreboot in ", tostring(t) .. "s");
                    os.sleep(1)
                    if t == 0 then break end
                end
                os.reboot()
            end
        end
        os.sleep(1);
    end
end
)
if(not sock) then log("error!!!, how did you get here ?");return end
__printHeader();
log("Transmitting identity")
send({Identity=os.getComputerID(),Label=os.getComputerLabel()},"Identity")
__printHeader();
parallel.waitForAny(function()
    message_dequeue()
end);