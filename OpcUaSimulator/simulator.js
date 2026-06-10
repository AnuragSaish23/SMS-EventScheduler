const { OPCUAServer, Variant, DataType } = require("node-opcua");

// Configurable via environment variable (default: 3 seconds)
const SIGNAL_INTERVAL_MS = parseInt(process.env.SIGNAL_INTERVAL_MS || "3000", 10);

async function main() {
    const server = new OPCUAServer({
        port: 4840,
        resourcePath: "/UA/FactorySimulator",
        buildInfo: { productName: "SMS Factory Simulator", buildDate: new Date() }
    });

    await server.initialize();

    const addressSpace = server.engine.addressSpace;
    const ns = addressSpace.getOwnNamespace();
    const folder = ns.addFolder(addressSpace.rootFolder.objects, { browseName: "FactoryFloor" });

    // Sensor state
    let pumpVal = false, setupVal = false, completeVal = false;

        const pumpSensor = ns.addVariable({
        componentOf: folder, browseName: "PumpSensor", nodeId: "s=PumpSensor", dataType: "Boolean",
        value: { get: () => new Variant({ dataType: DataType.Boolean, value: pumpVal }) }
    });

    const setupSensor = ns.addVariable({
        componentOf: folder, browseName: "SetupSensor", nodeId: "s=SetupSensor", dataType: "Boolean",
        value: { get: () => new Variant({ dataType: DataType.Boolean, value: setupVal }) }
    });

    const completeSensor = ns.addVariable({
        componentOf: folder, browseName: "OperationComplete", nodeId: "s=OperationComplete", dataType: "Boolean",
        value: { get: () => new Variant({ dataType: DataType.Boolean, value: completeVal }) }
    });

    await server.start();
    console.log(`\n========================================`);
    console.log(`  SMS Factory Simulator is RUNNING`);
    console.log(`  Endpoint: ${server.getEndpointUrl()}`);
    console.log(`========================================\n`);
    console.log(`Signal interval: ${SIGNAL_INTERVAL_MS}ms (set SIGNAL_INTERVAL_MS to change)`);
    console.log(`Sensors will flip randomly every ${SIGNAL_INTERVAL_MS/1000}-${SIGNAL_INTERVAL_MS*2/1000}s...\n`);

    // Simulation loop: randomly flip sensors
    const simulate = () => {
        const rand = Math.random();

        if (rand < 0.35) {
            // Pump breaks or recovers
            pumpVal = !pumpVal;
            pumpSensor.setValueFromSource(new Variant({ dataType: DataType.Boolean, value: pumpVal }));
            console.log(`[${new Date().toLocaleTimeString()}] ${pumpVal ? "🔴 PUMP BROKE!" : "🟢 Pump fixed"} (PumpSensor = ${pumpVal})`);
        } else if (rand < 0.60) {
            // Setup change
            setupVal = !setupVal;
            setupSensor.setValueFromSource(new Variant({ dataType: DataType.Boolean, value: setupVal }));
            console.log(`[${new Date().toLocaleTimeString()}] ${setupVal ? "🟡 DIE CHANGE started" : "🟢 Die change done"} (SetupSensor = ${setupVal})`);
        } else {
            // Operation complete
            completeVal = !completeVal;
            completeSensor.setValueFromSource(new Variant({ dataType: DataType.Boolean, value: completeVal }));
            console.log(`[${new Date().toLocaleTimeString()}] ${completeVal ? "⚡ OPERATION COMPLETE signal" : "⏳ Waiting..."} (OperationComplete = ${completeVal})`);
        }

        // Next event in INTERVAL to 2*INTERVAL ms
        const nextDelay = SIGNAL_INTERVAL_MS + Math.floor(Math.random() * SIGNAL_INTERVAL_MS);
        setTimeout(simulate, nextDelay);
    };

    // Start after 2 seconds
    setTimeout(simulate, 2000);
}

main().catch(console.error);
