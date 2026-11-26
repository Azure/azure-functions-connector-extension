package com.microsoft.azure.functions.connector.tests;

import static org.junit.jupiter.api.Assertions.*;

import com.microsoft.azure.functions.ExecutionContext;
import com.microsoft.azure.functions.annotation.CustomBinding;
import com.microsoft.azure.functions.annotation.FunctionName;
import java.lang.reflect.Proxy;
import java.util.ArrayList;
import java.util.List;
import java.util.logging.Handler;
import java.util.logging.LogRecord;
import java.util.logging.Logger;
import org.junit.jupiter.api.Test;

class GenericFunctionTest {
    @Test
    void rawPayloadsDoNotRequireConnectorModels() {
        List<String> messages = new ArrayList<>();
        Logger logger = Logger.getAnonymousLogger();
        logger.setUseParentHandlers(false);
        logger.addHandler(new Handler() {
            public void publish(LogRecord record) { messages.add(record.getMessage()); }
            public void flush() {}
            public void close() {}
        });
        ExecutionContext context = (ExecutionContext) Proxy.newProxyInstance(
                ExecutionContext.class.getClassLoader(), new Class<?>[] { ExecutionContext.class },
                (proxy, method, args) -> {
                    if (method.getName().equals("getLogger")) { return logger; }
                    throw new UnsupportedOperationException(method.getName());
                });
        GenericFunction function = new GenericFunction();
        function.run("{\"body\":{\"arbitrary\":true}}", context);
        function.run("[{\"arbitrary\":true}]", context);
        assertEquals(List.of("Received generic Poll connector payload.", "Received generic Poll connector payload."), messages);
        for (String payload : new String[] { null, "", " " }) {
            assertThrows(IllegalArgumentException.class, () -> function.run(payload, context));
        }
        assertEquals(2, messages.size());
    }

    @Test
    void customBindingMapsTheRawStringParameter() throws Exception {
        var method = GenericFunction.class.getMethod("run", String.class, ExecutionContext.class);
        assertEquals("OnGenericPoll", method.getAnnotation(FunctionName.class).value());
        CustomBinding binding = method.getParameters()[0].getAnnotation(CustomBinding.class);
        assertEquals("payload", binding.name());
        assertEquals("connectorTrigger", binding.type());
        assertEquals("in", binding.direction());
    }
}
